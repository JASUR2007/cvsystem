using backend.Api;
using backend.Auth;
using backend.Data;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace backend.Controllers;

[ApiController]
[Route("api/positions")]
public sealed class PositionsController(AppDbContext db) : ControllerBase
{
    [HttpGet]
    public async Task<IActionResult> List(string? q, string? company, PositionLevel? level, int page = 1, int pageSize = 20, CancellationToken cancellationToken = default)
    {
        var query = db.Positions.AsNoTracking().AsQueryable();
        if (!User.Identity?.IsAuthenticated ?? true) query = query.Where(position => position.IsPublic);
        else if (User.IsInRole(Roles.Candidate) && !ApiModels.IsAdmin(User))
            query = PositionAccess.Eligible(query, db, ApiModels.UserId(User)!.Value);
        if (!string.IsNullOrWhiteSpace(q)) query = query.Where(position => EF.Functions.ILike(position.Title, "%" + q.Trim() + "%") || EF.Functions.ILike(position.ShortDescription, "%" + q.Trim() + "%"));
        if (!string.IsNullOrWhiteSpace(company)) query = query.Where(position => position.Company == company);
        if (level is not null) query = query.Where(position => position.Level == level);
        var currentPage = ApiModels.Page(page);
        var size = ApiModels.PageSize(pageSize);
        var total = await query.CountAsync(cancellationToken);
        var items = await query.OrderByDescending(position => position.UpdatedAt).ThenBy(position => position.Id)
            .Skip((currentPage - 1) * size).Take(size)
            .Select(position => new PositionListItem(position.Id, position.Title, position.Company, position.Level,
                position.UpdatedAt, position.IsPublic, position.Cvs.Count(cv => cv.Status == CvStatus.Published)))
            .ToListAsync(cancellationToken);
        return Ok(new PageResult<PositionListItem>(items, currentPage, size, total));
    }

    [HttpGet("{id:guid}")]
    public async Task<IActionResult> Get(Guid id, CancellationToken cancellationToken)
    {
        var position = await VisiblePositions().AsNoTracking()
            .Include(item => item.Attributes).ThenInclude(link => link.Attribute)
            .Include(item => item.AccessRules).ThenInclude(rule => rule.Attribute)
            .Include(item => item.ProjectTags).ThenInclude(link => link.Tag)
            .AsSplitQuery().FirstOrDefaultAsync(item => item.Id == id, cancellationToken);
        return position is null ? NotFound() : Ok(View(position));
    }

    [Authorize(Roles = Roles.Recruiter + "," + Roles.Administrator)]
    [HttpPost]
    public async Task<IActionResult> Create(PositionRequest request, CancellationToken cancellationToken)
    {
        var error = await Validate(request, cancellationToken);
        if (error is not null) return BadRequest(new { message = error });
        var position = new Position { Id = Guid.NewGuid() };
        db.Positions.Add(position);
        await Apply(position, request, cancellationToken);
        await db.SaveChangesAsync(cancellationToken);
        db.ChangeTracker.Clear();
        return CreatedAtAction(nameof(Get), new { id = position.Id }, View((await Template(position.Id, cancellationToken))!));
    }

    [Authorize(Roles = Roles.Recruiter + "," + Roles.Administrator)]
    [HttpPut("{id:guid}")]
    public async Task<IActionResult> Update(Guid id, PositionRequest request, CancellationToken cancellationToken)
    {
        var position = await Template(id, cancellationToken);
        if (position is null) return NotFound();
        if (position.Version != request.Version) return Conflict(new { message = "Position was changed in another session." });
        var error = await Validate(request, cancellationToken);
        if (error is not null) return BadRequest(new { message = error });
        await Apply(position, request, cancellationToken);
        position.Version++;
        try
        {
            await db.SaveChangesAsync(cancellationToken);
        }
        catch (DbUpdateConcurrencyException)
        {
            return Conflict(new { message = "Position was changed in another session." });
        }
        db.ChangeTracker.Clear();
        return Ok(View((await Template(id, cancellationToken))!));
    }

    [Authorize(Roles = Roles.Recruiter + "," + Roles.Administrator)]
    [HttpPost("{id:guid}/duplicate")]
    public async Task<IActionResult> Duplicate(Guid id, CancellationToken cancellationToken)
    {
        var source = await Template(id, cancellationToken);
        if (source is null) return NotFound();
        var request = new PositionRequest(source.Title + " (copy)", source.ShortDescription, source.Company, source.Level,
            source.IsPublic, source.MaxProjects,
            source.Attributes.Select(link => new PositionAttributeInput(link.AttributeId, link.SortOrder, link.IsRequired)).ToList(),
            source.AccessRules.Select(rule => new AccessRuleInput(rule.AttributeId, rule.Operator, rule.ComparisonValue)).ToList(),
            source.ProjectTags.Select(link => link.Tag.Name).ToList(), 1);
        var copy = new Position { Id = Guid.NewGuid() };
        db.Positions.Add(copy);
        await Apply(copy, request, cancellationToken);
        await db.SaveChangesAsync(cancellationToken);
        db.ChangeTracker.Clear();
        return CreatedAtAction(nameof(Get), new { id = copy.Id }, View((await Template(copy.Id, cancellationToken))!));
    }

    [Authorize(Roles = Roles.Recruiter + "," + Roles.Administrator)]
    [HttpDelete("{id:guid}")]
    public async Task<IActionResult> Delete(Guid id, CancellationToken cancellationToken)
    {
        var position = await db.Positions.FindAsync([id], cancellationToken);
        if (position is null) return NotFound();
        if (await db.Cvs.AnyAsync(cv => cv.PositionId == id, cancellationToken))
            return Conflict(new { message = "Position has CVs and cannot be deleted." });
        db.Positions.Remove(position);
        await db.SaveChangesAsync(cancellationToken);
        return NoContent();
    }

    private IQueryable<Position> VisiblePositions()
    {
        var query = db.Positions.AsQueryable();
        if (!User.Identity?.IsAuthenticated ?? true) return query.Where(position => position.IsPublic);
        if (User.IsInRole(Roles.Candidate) && !ApiModels.IsAdmin(User))
            return PositionAccess.Eligible(query, db, ApiModels.UserId(User)!.Value);
        return query;
    }

    private Task<Position?> Template(Guid id, CancellationToken cancellationToken) => db.Positions
        .Include(position => position.Attributes).ThenInclude(link => link.Attribute)
        .Include(position => position.AccessRules).ThenInclude(rule => rule.Attribute)
        .Include(position => position.ProjectTags).ThenInclude(link => link.Tag)
        .AsSplitQuery().FirstOrDefaultAsync(position => position.Id == id, cancellationToken);

    private async Task<string?> Validate(PositionRequest request, CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(request.Title) || request.Title.Length > 200) return "Title is required and must be at most 200 characters.";
        if (request.ShortDescription?.Length > 2000 || request.Company?.Length > 160) return "Description or company is too long.";
        if (request.MaxProjects is < 0 or > 20) return "Maximum projects must be between 0 and 20.";
        if (!request.IsPublic && request.AccessRules.Count == 0) return "Restricted positions need at least one access rule.";
        if (request.Attributes.Select(item => item.AttributeId).Distinct().Count() != request.Attributes.Count) return "Duplicate attributes are not allowed.";
        if (request.ProjectTags.Count > 20 || request.ProjectTags.Any(tag => string.IsNullOrWhiteSpace(tag) || tag.Length > 80)) return "Invalid project tags.";

        var ids = request.Attributes.Select(item => item.AttributeId).Concat(request.AccessRules.Select(rule => rule.AttributeId)).Distinct().ToList();
        var attributes = await db.Attributes.Include(item => item.Options).Where(item => ids.Contains(item.Id)).ToDictionaryAsync(item => item.Id, cancellationToken);
        if (attributes.Count != ids.Count) return "An attribute was not found.";
        foreach (var rule in request.AccessRules)
        {
            var error = PositionAccess.ValidateRule(attributes[rule.AttributeId], rule.Operator, rule.ComparisonValue);
            if (error is not null) return error;
        }
        return null;
    }

    private async Task Apply(Position position, PositionRequest request, CancellationToken cancellationToken)
    {
        position.Title = request.Title.Trim();
        position.ShortDescription = request.ShortDescription?.Trim() ?? string.Empty;
        position.Company = request.Company?.Trim();
        position.Level = request.Level;
        position.IsPublic = request.IsPublic;
        position.MaxProjects = request.MaxProjects;
        position.UpdatedAt = DateTime.UtcNow;

        var requestedIds = request.Attributes.Select(item => item.AttributeId).ToHashSet();
        var removedAttributes = position.Attributes.Where(link => !requestedIds.Contains(link.AttributeId)).ToList();
        db.PositionAttributes.RemoveRange(removedAttributes);
        foreach (var link in removedAttributes) position.Attributes.Remove(link);
        foreach (var item in request.Attributes)
        {
            var link = position.Attributes.FirstOrDefault(value => value.AttributeId == item.AttributeId);
            if (link is null) position.Attributes.Add(new PositionAttribute { Position = position, AttributeId = item.AttributeId, SortOrder = item.SortOrder, IsRequired = item.IsRequired });
            else { link.SortOrder = item.SortOrder; link.IsRequired = item.IsRequired; }
        }

        db.PositionAccessRules.RemoveRange(position.AccessRules);
        position.AccessRules.Clear();
        var ruleAttributes = await db.Attributes.Where(attribute => request.AccessRules.Select(rule => rule.AttributeId).Contains(attribute.Id))
            .ToDictionaryAsync(attribute => attribute.Id, cancellationToken);
        foreach (var rule in request.AccessRules)
        {
            var type = ruleAttributes[rule.AttributeId].Type;
            position.AccessRules.Add(new PositionAccessRule
            {
                Id = Guid.NewGuid(), Position = position, AttributeId = rule.AttributeId,
                Operator = rule.Operator, ComparisonValue = rule.ComparisonValue,
                NumberValue = type == AttributeType.Numeric ? decimal.Parse(rule.ComparisonValue, System.Globalization.CultureInfo.InvariantCulture) : null,
                BooleanValue = type == AttributeType.Boolean ? bool.Parse(rule.ComparisonValue) : null,
                OptionId = type == AttributeType.Dropdown ? Guid.Parse(rule.ComparisonValue) : null
            });
        }

        var tagNames = request.ProjectTags.Select(tag => tag.Trim()).Distinct(StringComparer.OrdinalIgnoreCase).ToList();
        var removedTags = position.ProjectTags.Where(link => !tagNames.Contains(link.Tag.Name, StringComparer.OrdinalIgnoreCase)).ToList();
        db.PositionProjectTags.RemoveRange(removedTags);
        foreach (var link in removedTags) position.ProjectTags.Remove(link);
        var missing = tagNames.Where(name => position.ProjectTags.All(link => !string.Equals(link.Tag.Name, name, StringComparison.OrdinalIgnoreCase))).ToList();
        var existingTags = await db.Tags.Where(tag => missing.Contains(tag.Name)).ToListAsync(cancellationToken);
        var tags = existingTags.Concat(missing.Where(name => existingTags.All(tag => !string.Equals(tag.Name, name, StringComparison.OrdinalIgnoreCase)))
            .Select(name => new Tag { Id = Guid.NewGuid(), Name = name }));
        foreach (var tag in tags) position.ProjectTags.Add(new PositionProjectTag { Position = position, Tag = tag, TagId = tag.Id });
    }

    private static PositionDetail View(Position position) => new(position.Id, position.Title, position.ShortDescription, position.Company,
        position.Level, position.IsPublic, position.MaxProjects, position.Version, position.CreatedAt, position.UpdatedAt,
        position.Attributes.OrderBy(link => link.SortOrder).Select(link => new PositionAttributeView(link.AttributeId, link.Attribute.Name, link.Attribute.Type, link.SortOrder, link.IsRequired)).ToList(),
        position.AccessRules.Select(rule => new AccessRuleView(rule.Id, rule.AttributeId, rule.Attribute.Name, rule.Operator, rule.ComparisonValue)).ToList(),
        position.ProjectTags.Select(link => link.Tag.Name).Order().ToList());
}

public sealed record PositionRequest(string Title, string? ShortDescription, string? Company, PositionLevel? Level, bool IsPublic, int MaxProjects,
    List<PositionAttributeInput> Attributes, List<AccessRuleInput> AccessRules, List<string> ProjectTags, int Version);
public sealed record PositionAttributeInput(Guid AttributeId, int SortOrder, bool IsRequired);
public sealed record AccessRuleInput(Guid AttributeId, AccessOperator Operator, string ComparisonValue);
public sealed record PositionListItem(Guid Id, string Title, string? Company, PositionLevel? Level, DateTime UpdatedAt, bool IsPublic, int CvCount);
public sealed record PositionAttributeView(Guid AttributeId, string Name, AttributeType Type, int SortOrder, bool IsRequired);
public sealed record AccessRuleView(Guid Id, Guid AttributeId, string AttributeName, AccessOperator Operator, string ComparisonValue);
public sealed record PositionDetail(Guid Id, string Title, string ShortDescription, string? Company, PositionLevel? Level, bool IsPublic,
    int MaxProjects, int Version, DateTime CreatedAt, DateTime UpdatedAt, List<PositionAttributeView> Attributes,
    List<AccessRuleView> AccessRules, List<string> ProjectTags);
