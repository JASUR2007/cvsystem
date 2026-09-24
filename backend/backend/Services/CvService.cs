using backend.Auth;
using backend.Common.Enums;
using backend.Common.Exceptions;
using backend.Data;
using backend.DTOs.Attributes;
using backend.DTOs.Cvs;
using backend.DTOs.Profile;
using backend.DTOs.Projects;
using backend.Entities;
using backend.Services.Interfaces;
using Microsoft.EntityFrameworkCore;

namespace backend.Services;

public class CvService(AppDbContext db, ICurrentUserService currentUser) : ICvService {
    public async Task<List<CvListItem>> ListCandidateCvsAsync(Guid candidateId, CancellationToken cancellationToken = default) {
        var items = await db.Cvs.AsNoTracking().Where(cv => cv.CandidateId == candidateId)
            .OrderByDescending(cv => cv.UpdatedAt)
            .Select(cv => new CvListItem(
                cv.Id,
                cv.PositionId,
                cv.Position.Title,
                cv.Status,
                cv.UpdatedAt,
                cv.Likes.Count))
            .ToListAsync(cancellationToken);

        return items;
    }

    public async Task<CvDetail> CreateCvAsync(Guid positionId, Guid candidateId, CancellationToken cancellationToken = default) {
        var position = await db.Positions.Include(item => item.Attributes).FirstOrDefaultAsync(item => item.Id == positionId, cancellationToken);
        if (position is null)
            throw new NotFoundException("Position not found.");

        if (!await PositionAccessHelper.Eligible(db.Positions, db, candidateId).AnyAsync(item => item.Id == positionId, cancellationToken))
            throw new ForbiddenException("You are not eligible for this position.");

        if (await db.Cvs.AnyAsync(cv => cv.PositionId == positionId && cv.CandidateId == candidateId, cancellationToken))
            throw new ConflictException("You already created a CV for this position.");

        var existingValues = await db.UserAttributeValues.Where(value => value.UserId == candidateId)
            .Select(value => value.AttributeId).ToListAsync(cancellationToken);

        var missing = position.Attributes.Select(attribute => attribute.AttributeId)
            .Except(existingValues)
            .Select(attributeId => new UserAttributeValue { UserId = candidateId, AttributeId = attributeId })
            .ToList();

        if (missing.Count > 0) {
            db.UserAttributeValues.AddRange(missing);
        }

        var cv = new Cv {
            Id = Guid.NewGuid(),
            PositionId = positionId,
            CandidateId = candidateId,
            Status = CvStatus.Draft
        };

        db.Cvs.Add(cv);
        await db.SaveChangesAsync(cancellationToken);

        return await GetCvDetailAsync(cv.Id, cancellationToken);
    }

    public async Task<CvDetail> GetCvDetailAsync(Guid id, CancellationToken cancellationToken = default) {
        var cv = await db.Cvs.AsNoTracking()
            .Include(item => item.Candidate)
            .Include(item => item.Position).ThenInclude(position => position.ProjectTags).ThenInclude(link => link.Tag)
            .Include(item => item.Position).ThenInclude(position => position.Attributes).ThenInclude(link => link.Attribute).ThenInclude(attribute => attribute.Options)
            .FirstOrDefaultAsync(item => item.Id == id, cancellationToken);

        if (cv is null)
            throw new NotFoundException("CV not found.");

        var currentUserId = currentUser.UserId;
        var canEdit = currentUserId == cv.CandidateId || currentUser.IsAdmin;

        if (!canEdit && cv.Status != CvStatus.Published)
            throw new NotFoundException("CV is not available.");

        if (!currentUser.IsAdmin && !await PositionAccessHelper.EligibleCvs(db.Cvs.Where(item => item.Id == id), db).AnyAsync(cancellationToken))
            throw new NotFoundException("CV is not available.");

        var attributeIds = cv.Position.Attributes.Select(attribute => attribute.AttributeId).ToList();
        var userValues = await db.UserAttributeValues.AsNoTracking()
            .Where(value => value.UserId == cv.CandidateId && attributeIds.Contains(value.AttributeId))
            .ToDictionaryAsync(value => value.AttributeId, cancellationToken);

        var attributes = cv.Position.Attributes.OrderBy(attribute => attribute.SortOrder).Select(attribute => {
            var value = userValues.GetValueOrDefault(attribute.AttributeId);
            return new CvAttributeView(
                AttributeValueHelper.View(attribute.Attribute, value, cv.Candidate),
                attribute.IsRequired,
                AttributeValueHelper.IsFilled(value, attribute.Attribute, cv.Candidate),
                attribute.Attribute.Options.OrderBy(option => option.SortOrder).Select(option => new AttributeOptionView(option.Id, option.Value)).ToList());
        }).ToList();

        var tags = cv.Position.ProjectTags.Select(tag => tag.Tag.Name).ToList();
        var projects = await db.Projects.AsNoTracking().Where(project => project.UserId == cv.CandidateId)
            .Include(project => project.Tags).ThenInclude(link => link.Tag)
            .ToListAsync(cancellationToken);

        var filteredProjects = projects
            .OrderByDescending(project => project.Tags.Count(tag => tags.Contains(tag.Tag.Name, StringComparer.OrdinalIgnoreCase)))
            .ThenByDescending(project => project.StartedOn)
            .Take(cv.Position.MaxProjects)
            .Select(project => new ProjectView(
                project.Id,
                project.Name,
                project.StartedOn,
                project.EndedOn,
                project.Description,
                project.Tags.Select(link => link.Tag.Name).Order().ToList(),
                project.Version))
            .ToList();

        var likesCount = await db.CvLikes.CountAsync(like => like.CvId == cv.Id, cancellationToken);

        return new CvDetail(
            cv.Id,
            cv.PositionId,
            cv.Position.Title,
            cv.CandidateId,
            cv.Candidate.FirstName,
            cv.Candidate.LastName,
            cv.Candidate.Location,
            cv.Candidate.PhotoObjectKey,
            cv.Status,
            cv.UpdatedAt,
            likesCount,
            canEdit,
            attributes,
            filteredProjects);
    }

    public async Task<AttributeValueView> UpdateCvAttributeAsync(Guid id, Guid attributeId, AttributeValueUpdate request, CancellationToken cancellationToken = default) {
        var cv = await EditableCvAsync(id, cancellationToken);
        if (cv is null)
            throw new NotFoundException("CV not found.");

        var attribute = await db.Attributes.Include(item => item.Options).FirstOrDefaultAsync(item => item.Id == attributeId, cancellationToken);
        if (attribute is null || !await db.PositionAttributes.AnyAsync(link => link.PositionId == cv.PositionId && link.AttributeId == attributeId, cancellationToken))
            throw new NotFoundException("Attribute not found on this position.");

        var error = AttributeValueHelper.Validate(attribute, request.Value);
        if (error is not null)
            throw new ValidationException(error);

        if (!AttributeValueHelper.ValidImageKey(request.Value.ImageObjectKey, cv.CandidateId))
            throw new ValidationException("Image must belong to this profile.");

        UserAttributeValue? value = null;
        if (attribute.IsBuiltIn) {
            var user = cv.Candidate;
            if (user.Version != request.Version)
                throw new ConflictException("Profile was changed in another session.");

            switch (attribute.Name) {
                case "First Name": user.FirstName = request.Value.TextValue?.Trim() ?? string.Empty; break;
                case "Last Name": user.LastName = request.Value.TextValue?.Trim() ?? string.Empty; break;
                case "Location": user.Location = request.Value.TextValue?.Trim(); break;
                case "Personal Photo": user.PhotoObjectKey = AttributeValueHelper.CleanImageKey(request.Value.ImageObjectKey); break;
            }

            if (string.IsNullOrWhiteSpace(user.FirstName) || string.IsNullOrWhiteSpace(user.LastName))
                throw new ValidationException("Name is required.");

            user.Version++;
        }
        else {
            value = await db.UserAttributeValues.FirstOrDefaultAsync(item => item.UserId == cv.CandidateId && item.AttributeId == attributeId, cancellationToken);
            if (value is null)
                throw new NotFoundException("Attribute value not found.");

            if (value.Version != request.Version)
                throw new ConflictException("Attribute was changed in another session.");

            AttributeValueHelper.Apply(value, attribute, request.Value);
        }

        cv.UpdatedAt = DateTime.UtcNow;
        await db.SaveChangesAsync(cancellationToken);

        return AttributeValueHelper.View(attribute, value, cv.Candidate);
    }

    public async Task<object> PublishCvAsync(Guid id, CancellationToken cancellationToken = default) {
        var cv = await EditableCvAsync(id, cancellationToken);
        if (cv is null)
            throw new NotFoundException("CV not found.");

        if (!await PositionAccessHelper.Eligible(db.Positions, db, cv.CandidateId).AnyAsync(position => position.Id == cv.PositionId, cancellationToken))
            throw new ForbiddenException("You are not eligible for this position.");

        var required = await db.PositionAttributes.AsNoTracking().Where(link => link.PositionId == cv.PositionId && link.IsRequired)
            .Include(link => link.Attribute).ToListAsync(cancellationToken);

        var ids = required.Select(link => link.AttributeId).ToList();
        var values = await db.UserAttributeValues.AsNoTracking().Where(value => value.UserId == cv.CandidateId && ids.Contains(value.AttributeId))
            .ToDictionaryAsync(value => value.AttributeId, cancellationToken);

        var missing = required.Where(link => !AttributeValueHelper.IsFilled(values.GetValueOrDefault(link.AttributeId), link.Attribute, cv.Candidate))
            .Select(link => link.Attribute.Name).ToList();

        if (missing.Count > 0)
            throw new ValidationException($"Fill all required attributes before publishing: {string.Join(", ", missing)}");

        cv.Status = CvStatus.Published;
        cv.PublishedAt = DateTime.UtcNow;
        cv.UpdatedAt = DateTime.UtcNow;
        await db.SaveChangesAsync(cancellationToken);

        return new { cv.Id, cv.Status, cv.PublishedAt };
    }

    public async Task DeleteCvAsync(Guid id, CancellationToken cancellationToken = default) {
        var cv = await EditableCvAsync(id, cancellationToken);
        if (cv is null)
            throw new NotFoundException("CV not found.");

        db.Cvs.Remove(cv);
        await db.SaveChangesAsync(cancellationToken);
    }

    private Task<Cv?> EditableCvAsync(Guid id, CancellationToken cancellationToken) {
        var query = db.Cvs.Include(cv => cv.Candidate).Where(cv => cv.Id == id);
        if (!currentUser.IsAdmin) {
            var currentId = currentUser.RequireUserId();
            query = query.Where(cv => cv.CandidateId == currentId && PositionAccessHelper.Eligible(db.Positions, db, currentId)
                .Any(position => position.Id == cv.PositionId));
        }
        return query.FirstOrDefaultAsync(cancellationToken);
    }
}
