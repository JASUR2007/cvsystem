using backend.Api;
using backend.Auth;
using backend.Data;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Npgsql;

namespace backend.Controllers;

[ApiController]
[Authorize]
public sealed class CvsController(AppDbContext db) : ControllerBase
{
    [Authorize(Roles = Roles.Candidate + "," + Roles.Administrator)]
    [HttpPost("api/positions/{positionId:guid}/cvs")]
    public async Task<IActionResult> Create(Guid positionId, Guid? candidateId, CancellationToken cancellationToken)
    {
        var currentId = ApiModels.UserId(User);
        var ownerId = ApiModels.IsAdmin(User) && candidateId is not null ? candidateId : currentId;
        if (ownerId is null) return Unauthorized();
        if (!await db.Users.AnyAsync(user => user.Id == ownerId, cancellationToken)) return NotFound(new { message = "Candidate was not found." });
        var position = await PositionAccess.Eligible(db.Positions.Include(item => item.Attributes), db, ownerId.Value)
            .FirstOrDefaultAsync(item => item.Id == positionId, cancellationToken);
        if (position is null) return NotFound(new { message = "Position is not available." });
        if (await db.Cvs.AnyAsync(cv => cv.CandidateId == ownerId && cv.PositionId == positionId, cancellationToken))
            return Conflict(new { message = "A CV for this position already exists." });

        var selectedIds = position.Attributes.Select(link => link.AttributeId).ToList();
        var missing = selectedIds.Except(await db.UserAttributeValues.Where(value => value.UserId == ownerId && selectedIds.Contains(value.AttributeId))
            .Select(value => value.AttributeId).ToListAsync(cancellationToken)).ToList();
        var builtInIds = await db.Attributes.Where(attribute => attribute.IsBuiltIn && missing.Contains(attribute.Id))
            .Select(attribute => attribute.Id).ToListAsync(cancellationToken);

        await using var transaction = await db.Database.BeginTransactionAsync(cancellationToken);
        db.UserAttributeValues.AddRange(missing.Except(builtInIds).Select(id => new UserAttributeValue { UserId = ownerId.Value, AttributeId = id }));
        var cv = new Cv { Id = Guid.NewGuid(), CandidateId = ownerId.Value, PositionId = positionId };
        db.Cvs.Add(cv);
        try
        {
            await db.SaveChangesAsync(cancellationToken);
            await transaction.CommitAsync(cancellationToken);
        }
        catch (DbUpdateException exception) when (exception.InnerException is PostgresException { SqlState: PostgresErrorCodes.UniqueViolation })
        {
            return Conflict(new { message = "A CV for this position already exists." });
        }

        return CreatedAtAction(nameof(Get), new { id = cv.Id }, new { cv.Id, cv.Status });
    }

    [Authorize(Roles = Roles.Candidate + "," + Roles.Administrator)]
    [HttpGet("api/cvs")]
    public async Task<IActionResult> List(Guid? candidateId, CancellationToken cancellationToken)
    {
        var currentId = ApiModels.UserId(User);
        var ownerId = ApiModels.IsAdmin(User) && candidateId is not null ? candidateId : currentId;
        if (ownerId is null) return Unauthorized();
        var query = db.Cvs.AsNoTracking().Where(cv => cv.CandidateId == ownerId);
        if (!ApiModels.IsAdmin(User)) query = query.Where(cv => cv.Position.IsPublic || PositionAccess.Eligible(db.Positions, db, ownerId.Value).Any(position => position.Id == cv.PositionId));
        var items = await query.OrderByDescending(cv => cv.UpdatedAt)
            .Select(cv => new CvListItem(cv.Id, cv.PositionId, cv.Position.Title, cv.Status, cv.UpdatedAt, cv.Likes.Count))
            .ToListAsync(cancellationToken);
        return Ok(items);
    }

    [HttpGet("api/cvs/{id:guid}")]
    public async Task<IActionResult> Get(Guid id, CancellationToken cancellationToken)
    {
        var cv = await db.Cvs.AsNoTracking().Include(item => item.Candidate)
            .Include(item => item.Likes)
            .Include(item => item.Position).ThenInclude(position => position.Attributes).ThenInclude(link => link.Attribute).ThenInclude(attribute => attribute.Options)
            .Include(item => item.Position).ThenInclude(position => position.ProjectTags).ThenInclude(link => link.Tag)
            .AsSplitQuery().FirstOrDefaultAsync(item => item.Id == id, cancellationToken);
        if (cv is null) return NotFound();
        var currentId = ApiModels.UserId(User);
        var canEdit = ApiModels.IsAdmin(User) || (User.IsInRole(Roles.Candidate) && cv.CandidateId == currentId);
        if (!canEdit && (!User.IsInRole(Roles.Recruiter) || cv.Status != CvStatus.Published)) return Forbid();
        if (!ApiModels.IsAdmin(User)
            && !await PositionAccess.EligibleCvs(db.Cvs, db).AnyAsync(item => item.Id == id, cancellationToken))
            return NotFound();

        var attributeIds = cv.Position.Attributes.Select(link => link.AttributeId).ToList();
        var values = await db.UserAttributeValues.AsNoTracking().Where(value => value.UserId == cv.CandidateId && attributeIds.Contains(value.AttributeId))
            .ToDictionaryAsync(value => value.AttributeId, cancellationToken);
        var attributes = cv.Position.Attributes.OrderBy(link => link.SortOrder)
            .Select(link => new CvAttributeView(AttributeValues.View(link.Attribute, values.GetValueOrDefault(link.AttributeId), cv.Candidate), link.IsRequired,
                AttributeValues.IsFilled(values.GetValueOrDefault(link.AttributeId), link.Attribute, cv.Candidate),
                link.Attribute.Options.OrderBy(option => option.SortOrder).Select(option => new AttributeOptionView(option.Id, option.Value)).ToList()))
            .ToList();

        var requiredTags = cv.Position.ProjectTags.Select(link => link.TagId).ToList();
        var projectsQuery = db.Projects.AsNoTracking().Where(project => project.UserId == cv.CandidateId);
        if (requiredTags.Count > 0) projectsQuery = projectsQuery.Where(project => project.Tags.Any(link => requiredTags.Contains(link.TagId)));
        var projects = await projectsQuery.OrderByDescending(project => project.StartedOn).Take(cv.Position.MaxProjects)
            .Include(project => project.Tags).ThenInclude(link => link.Tag).ToListAsync(cancellationToken);

        return Ok(new CvDetail(cv.Id, cv.PositionId, cv.Position.Title, cv.CandidateId,
            cv.Candidate.FirstName, cv.Candidate.LastName, cv.Candidate.Location, cv.Candidate.PhotoObjectKey,
            cv.Status, cv.UpdatedAt, cv.Likes.Count, canEdit, attributes,
            projects.Select(project => new ProjectView(project.Id, project.Name, project.StartedOn, project.EndedOn,
                project.Description, project.Tags.Select(link => link.Tag.Name).ToList(), project.Version)).ToList()));
    }

    [Authorize(Roles = Roles.Candidate + "," + Roles.Administrator)]
    [HttpPut("api/cvs/{id:guid}/attributes/{attributeId:guid}")]
    public async Task<IActionResult> UpdateAttribute(Guid id, Guid attributeId, AttributeValueUpdate request, CancellationToken cancellationToken)
    {
        var cv = await EditableCv(id, cancellationToken);
        if (cv is null) return NotFound();
        var attribute = await db.Attributes.Include(item => item.Options).FirstOrDefaultAsync(item => item.Id == attributeId, cancellationToken);
        if (attribute is null || !await db.PositionAttributes.AnyAsync(link => link.PositionId == cv.PositionId && link.AttributeId == attributeId, cancellationToken)) return NotFound();
        var error = AttributeValues.Validate(attribute, request.Value);
        if (error is not null) return BadRequest(new { message = error });
        if (!ApiModels.ValidImageKey(request.Value.ImageObjectKey, cv.CandidateId))
            return BadRequest(new { message = "Image must belong to this profile." });

        UserAttributeValue? value = null;
        if (attribute.IsBuiltIn)
        {
            var user = cv.Candidate;
            if (user.Version != request.Version) return Conflict(new { message = "Profile was changed in another session." });
            switch (attribute.Name)
            {
                case "First Name": user.FirstName = request.Value.TextValue?.Trim() ?? string.Empty; break;
                case "Last Name": user.LastName = request.Value.TextValue?.Trim() ?? string.Empty; break;
                case "Location": user.Location = request.Value.TextValue?.Trim(); break;
                case "Personal Photo": user.PhotoObjectKey = request.Value.ImageObjectKey?.Trim(); break;
            }
            if (string.IsNullOrWhiteSpace(user.FirstName) || string.IsNullOrWhiteSpace(user.LastName)) return BadRequest(new { message = "Name is required." });
            user.Version++;
        }
        else
        {
            value = await db.UserAttributeValues.FirstOrDefaultAsync(item => item.UserId == cv.CandidateId && item.AttributeId == attributeId, cancellationToken);
            if (value is null) return NotFound();
            if (value.Version != request.Version) return Conflict(new { message = "Attribute was changed in another session." });
            AttributeValues.Apply(value, attribute, request.Value);
        }

        cv.UpdatedAt = DateTime.UtcNow;
        try
        {
            await db.SaveChangesAsync(cancellationToken);
        }
        catch (DbUpdateConcurrencyException)
        {
            return Conflict(new { message = "Attribute was changed in another session." });
        }
        return Ok(AttributeValues.View(attribute, value, cv.Candidate));
    }

    [Authorize(Roles = Roles.Candidate + "," + Roles.Administrator)]
    [HttpPost("api/cvs/{id:guid}/publish")]
    public async Task<IActionResult> Publish(Guid id, CancellationToken cancellationToken)
    {
        var cv = await EditableCv(id, cancellationToken);
        if (cv is null) return NotFound();
        if (!await PositionAccess.Eligible(db.Positions, db, cv.CandidateId).AnyAsync(position => position.Id == cv.PositionId, cancellationToken))
            return Forbid();
        var required = await db.PositionAttributes.AsNoTracking().Where(link => link.PositionId == cv.PositionId && link.IsRequired)
            .Include(link => link.Attribute).ToListAsync(cancellationToken);
        var ids = required.Select(link => link.AttributeId).ToList();
        var values = await db.UserAttributeValues.AsNoTracking().Where(value => value.UserId == cv.CandidateId && ids.Contains(value.AttributeId))
            .ToDictionaryAsync(value => value.AttributeId, cancellationToken);
        var missing = required.Where(link => !AttributeValues.IsFilled(values.GetValueOrDefault(link.AttributeId), link.Attribute, cv.Candidate))
            .Select(link => link.Attribute.Name).ToList();
        if (missing.Count > 0) return BadRequest(new { message = "Fill all required attributes before publishing.", missing });
        cv.Status = CvStatus.Published;
        cv.PublishedAt = DateTime.UtcNow;
        cv.UpdatedAt = DateTime.UtcNow;
        await db.SaveChangesAsync(cancellationToken);
        return Ok(new { cv.Id, cv.Status, cv.PublishedAt });
    }

    [Authorize(Roles = Roles.Candidate + "," + Roles.Administrator)]
    [HttpDelete("api/cvs/{id:guid}")]
    public async Task<IActionResult> Delete(Guid id, CancellationToken cancellationToken)
    {
        var cv = await EditableCv(id, cancellationToken);
        if (cv is null) return NotFound();
        db.Cvs.Remove(cv);
        await db.SaveChangesAsync(cancellationToken);
        return NoContent();
    }

    private Task<Cv?> EditableCv(Guid id, CancellationToken cancellationToken)
    {
        var query = db.Cvs.Include(cv => cv.Candidate).Where(cv => cv.Id == id);
        if (!ApiModels.IsAdmin(User))
        {
            var currentId = ApiModels.UserId(User);
            query = query.Where(cv => cv.CandidateId == currentId && PositionAccess.Eligible(db.Positions, db, currentId!.Value)
                .Any(position => position.Id == cv.PositionId));
        }
        return query.FirstOrDefaultAsync(cancellationToken);
    }
}

public sealed record CvListItem(Guid Id, Guid PositionId, string Position, CvStatus Status, DateTime UpdatedAt, int Likes);
public sealed record CvAttributeView(AttributeValueView Value, bool IsRequired, bool IsFilled, List<AttributeOptionView> Options);
public sealed record CvDetail(Guid Id, Guid PositionId, string Position, Guid CandidateId, string FirstName, string LastName,
    string? Location, string? PhotoObjectKey, CvStatus Status, DateTime UpdatedAt, int Likes, bool CanEdit,
    List<CvAttributeView> Attributes, List<ProjectView> Projects);
