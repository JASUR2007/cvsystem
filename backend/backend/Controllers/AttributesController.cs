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
[Route("api/attributes")]
public sealed class AttributesController(AppDbContext db) : ControllerBase
{
    private static readonly string[] Categories = ["Certification", "Domain Knowledge", "Education", "Personal Information", "Soft Skills", "Technical Skills"];

    [HttpGet("/api/attribute-categories")]
    public IActionResult GetCategories() => Ok(Categories);

    [HttpGet]
    public async Task<IActionResult> List(string? prefix, string? category, AttributeType? type, bool recent = false, int page = 1, int pageSize = 20, CancellationToken cancellationToken = default)
    {
        var query = db.Attributes.AsNoTracking().AsQueryable();
        if (!string.IsNullOrWhiteSpace(prefix)) query = query.Where(item => EF.Functions.ILike(item.Name, prefix.Trim() + "%"));
        if (!string.IsNullOrWhiteSpace(category)) query = query.Where(item => item.Category == category);
        if (type is not null) query = query.Where(item => item.Type == type);

        query = recent
            ? query.OrderByDescending(item => item.UserValues.Max(value => (DateTime?)value.UpdatedAt)).ThenBy(item => item.Name)
            : query.OrderBy(item => item.Name);

        var currentPage = ApiModels.Page(page);
        var size = ApiModels.PageSize(pageSize);
        var total = await query.CountAsync(cancellationToken);
        var items = await query.Skip((currentPage - 1) * size).Take(size)
            .Select(item => new AttributeListItem(item.Id, item.Name, item.Category, item.Type, item.IsBuiltIn, item.Version, item.PositionAttributes.Count))
            .ToListAsync(cancellationToken);
        return Ok(new PageResult<AttributeListItem>(items, currentPage, size, total));
    }

    [HttpGet("recent")]
    public Task<IActionResult> Recent(CancellationToken cancellationToken) => List(null, null, null, true, 1, 10, cancellationToken);

    [HttpGet("{id:guid}")]
    public async Task<IActionResult> Get(Guid id, CancellationToken cancellationToken)
    {
        var item = await db.Attributes.AsNoTracking().Include(attribute => attribute.Options)
            .FirstOrDefaultAsync(attribute => attribute.Id == id, cancellationToken);
        return item is null ? NotFound() : Ok(ToDetail(item));
    }

    [Authorize(Roles = Roles.Recruiter + "," + Roles.Administrator)]
    [HttpPost]
    public async Task<IActionResult> Create(AttributeRequest request, CancellationToken cancellationToken)
    {
        var error = ValidateRequest(request);
        if (error is not null) return BadRequest(new { message = error });

        var item = new AttributeDefinition
        {
            Id = Guid.NewGuid(),
            Name = request.Name.Trim(),
            Description = request.Description?.Trim() ?? string.Empty,
            Category = request.Category,
            Type = request.Type,
            Options = request.Type == AttributeType.Dropdown
                ? request.Options.Select((value, index) => new AttributeOption { Id = Guid.NewGuid(), Value = value.Trim(), SortOrder = index }).ToList()
                : []
        };
        db.Attributes.Add(item);
        try
        {
            await db.SaveChangesAsync(cancellationToken);
        }
        catch (DbUpdateException exception) when (exception.InnerException is PostgresException { SqlState: PostgresErrorCodes.UniqueViolation })
        {
            return Conflict(new { message = "Attribute name already exists." });
        }

        return CreatedAtAction(nameof(Get), new { id = item.Id }, ToDetail(item));
    }

    [Authorize(Roles = Roles.Recruiter + "," + Roles.Administrator)]
    [HttpPut("{id:guid}")]
    public async Task<IActionResult> Update(Guid id, AttributeRequest request, CancellationToken cancellationToken)
    {
        var error = ValidateRequest(request);
        if (error is not null) return BadRequest(new { message = error });

        var item = await db.Attributes.Include(attribute => attribute.Options)
            .FirstOrDefaultAsync(attribute => attribute.Id == id, cancellationToken);
        if (item is null) return NotFound();
        if (item.IsBuiltIn) return BadRequest(new { message = "Built-in attributes cannot be edited." });
        if (item.Version != request.Version) return Conflict(new { message = "Attribute was changed in another session." });

        if (item.Type != request.Type && await db.UserAttributeValues.AnyAsync(value => value.AttributeId == id, cancellationToken))
            return Conflict(new { message = "Attribute type cannot change while values exist." });

        if (item.Type == AttributeType.Dropdown && !item.Options.Select(option => option.Value).Order().SequenceEqual(request.Options.Select(option => option.Trim()).Order())
            && await db.UserAttributeValues.AnyAsync(value => value.AttributeId == id && value.SelectedOptionId != null, cancellationToken))
            return Conflict(new { message = "Options cannot change while selected values exist." });

        item.Name = request.Name.Trim();
        item.Description = request.Description?.Trim() ?? string.Empty;
        item.Category = request.Category;
        item.Type = request.Type;
        item.Version++;
        db.AttributeOptions.RemoveRange(item.Options);
        item.Options = request.Type == AttributeType.Dropdown
            ? request.Options.Select((value, index) => new AttributeOption { Id = Guid.NewGuid(), AttributeId = id, Value = value.Trim(), SortOrder = index }).ToList()
            : [];

        try
        {
            await db.SaveChangesAsync(cancellationToken);
        }
        catch (DbUpdateConcurrencyException)
        {
            return Conflict(new { message = "Attribute was changed in another session." });
        }
        catch (DbUpdateException exception) when (exception.InnerException is PostgresException { SqlState: PostgresErrorCodes.UniqueViolation })
        {
            return Conflict(new { message = "Attribute name already exists." });
        }

        return Ok(ToDetail(item));
    }

    [Authorize(Roles = Roles.Recruiter + "," + Roles.Administrator)]
    [HttpDelete("{id:guid}")]
    public async Task<IActionResult> Delete(Guid id, CancellationToken cancellationToken)
    {
        var item = await db.Attributes.FindAsync([id], cancellationToken);
        if (item is null) return NotFound();
        if (item.IsBuiltIn) return BadRequest(new { message = "Built-in attributes cannot be deleted." });
        if (await db.UserAttributeValues.AnyAsync(value => value.AttributeId == id, cancellationToken)
            || await db.PositionAttributes.AnyAsync(value => value.AttributeId == id, cancellationToken)
            || await db.PositionAccessRules.AnyAsync(value => value.AttributeId == id, cancellationToken))
            return Conflict(new { message = "Attribute is in use." });

        db.Attributes.Remove(item);
        await db.SaveChangesAsync(cancellationToken);
        return NoContent();
    }

    private static string? ValidateRequest(AttributeRequest request)
    {
        if (string.IsNullOrWhiteSpace(request.Name) || request.Name.Trim().Length > 160) return "Name is required and must be at most 160 characters.";
        if (!Categories.Contains(request.Category)) return "Invalid category.";
        if (!Enum.IsDefined(request.Type)) return "Invalid type.";
        if (request.Description?.Length > 2000) return "Description is too long.";
        if (request.Type == AttributeType.Dropdown && (request.Options.Count == 0 || request.Options.Any(option => string.IsNullOrWhiteSpace(option) || option.Length > 200)
            || request.Options.Distinct(StringComparer.OrdinalIgnoreCase).Count() != request.Options.Count)) return "Dropdown options must be unique and nonempty.";
        return null;
    }

    private static AttributeDetail ToDetail(AttributeDefinition item) => new(item.Id, item.Name, item.Description, item.Category, item.Type, item.IsBuiltIn, item.Version,
        item.Options.OrderBy(option => option.SortOrder).Select(option => new AttributeOptionView(option.Id, option.Value)).ToList());
}

public sealed record AttributeRequest(string Name, string? Description, string Category, AttributeType Type, List<string> Options, int Version);
public sealed record AttributeListItem(Guid Id, string Name, string Category, AttributeType Type, bool IsBuiltIn, int Version, int UsageCount);
public sealed record AttributeOptionView(Guid Id, string Value);
public sealed record AttributeDetail(Guid Id, string Name, string Description, string Category, AttributeType Type, bool IsBuiltIn, int Version, List<AttributeOptionView> Options);
