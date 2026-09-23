using backend.Common.Enums;
using backend.Common.Exceptions;
using backend.Common.Pagination;
using backend.Data;
using backend.DTOs.Attributes;
using backend.Entities;
using backend.Services.Interfaces;
using Microsoft.EntityFrameworkCore;

namespace backend.Services;

public class AttributeService(AppDbContext db) : IAttributeService
{
    private static readonly HashSet<string> Categories =
    [
        "Personal Information",
        "Language",
        "Technical Skills",
        "Soft Skills",
        "Certification",
        "Education",
        "Domain Knowledge"
    ];

    public async Task<PagedResult<AttributeListItem>> ListAsync(
        string? prefix, string? category, AttributeType? type, bool? recent,
        int page, int pageSize, CancellationToken cancellationToken = default)
    {
        var query = db.Attributes.AsNoTracking().AsQueryable();

        if (!string.IsNullOrWhiteSpace(prefix))
            query = query.Where(attribute => EF.Functions.ILike(attribute.Name, prefix.Trim() + "%"));

        if (!string.IsNullOrWhiteSpace(category))
            query = query.Where(attribute => attribute.Category == category);

        if (type is not null)
            query = query.Where(attribute => attribute.Type == type);

        var currentPage = Math.Max(1, page);
        var size = Math.Clamp(pageSize, 1, 100);
        var total = await query.CountAsync(cancellationToken);

        query = recent == true
            ? query.OrderByDescending(attribute => attribute.CreatedAt)
            : query.OrderBy(attribute => attribute.Name);

        var items = await query.Skip((currentPage - 1) * size).Take(size)
            .Select(attribute => new AttributeListItem(
                attribute.Id,
                attribute.Name,
                attribute.Category,
                attribute.Type,
                attribute.IsBuiltIn,
                attribute.Version,
                attribute.PositionAttributes.Count + attribute.UserValues.Count))
            .ToListAsync(cancellationToken);

        return new PagedResult<AttributeListItem>(items, currentPage, size, total);
    }

    public async Task<AttributeDetail> GetByIdAsync(Guid id, CancellationToken cancellationToken = default)
    {
        var item = await db.Attributes.AsNoTracking().Include(attribute => attribute.Options)
            .FirstOrDefaultAsync(attribute => attribute.Id == id, cancellationToken);
        if (item is null)
            throw new NotFoundException("Attribute not found.");

        return ToDetail(item);
    }

    public async Task<AttributeDetail> CreateAsync(AttributeRequest request, CancellationToken cancellationToken = default)
    {
        ValidateRequest(request);

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
        await db.SaveChangesAsync(cancellationToken);

        return ToDetail(item);
    }

    public async Task<AttributeDetail> UpdateAsync(Guid id, AttributeRequest request, CancellationToken cancellationToken = default)
    {
        ValidateRequest(request);

        var item = await db.Attributes.Include(attribute => attribute.Options)
            .FirstOrDefaultAsync(attribute => attribute.Id == id, cancellationToken);
        if (item is null)
            throw new NotFoundException("Attribute not found.");

        if (item.IsBuiltIn)
            throw new ValidationException("Built-in attributes cannot be edited.");

        if (item.Version != request.Version)
            throw new ConflictException("Attribute was changed in another session.");

        if (item.Type != request.Type && await db.UserAttributeValues.AnyAsync(value => value.AttributeId == id, cancellationToken))
            throw new ConflictException("Attribute type cannot change while values exist.");

        if (item.Type == AttributeType.Dropdown && !item.Options.Select(option => option.Value).Order().SequenceEqual(request.Options.Select(option => option.Trim()).Order())
            && await db.UserAttributeValues.AnyAsync(value => value.AttributeId == id && value.SelectedOptionId != null, cancellationToken))
            throw new ConflictException("Options cannot change while selected values exist.");

        item.Name = request.Name.Trim();
        item.Description = request.Description?.Trim() ?? string.Empty;
        item.Category = request.Category;
        item.Type = request.Type;
        item.Version++;

        db.AttributeOptions.RemoveRange(item.Options);
        item.Options = request.Type == AttributeType.Dropdown
            ? request.Options.Select((value, index) => new AttributeOption { Id = Guid.NewGuid(), AttributeId = id, Value = value.Trim(), SortOrder = index }).ToList()
            : [];

        await db.SaveChangesAsync(cancellationToken);
        return ToDetail(item);
    }

    public async Task DeleteAsync(Guid id, CancellationToken cancellationToken = default)
    {
        var item = await db.Attributes.FindAsync([id], cancellationToken);
        if (item is null)
            throw new NotFoundException("Attribute not found.");

        if (item.IsBuiltIn)
            throw new ValidationException("Built-in attributes cannot be deleted.");

        if (await db.UserAttributeValues.AnyAsync(value => value.AttributeId == id, cancellationToken)
            || await db.PositionAttributes.AnyAsync(value => value.AttributeId == id, cancellationToken)
            || await db.PositionAccessRules.AnyAsync(value => value.AttributeId == id, cancellationToken))
            throw new ConflictException("Attribute is in use.");

        db.Attributes.Remove(item);
        await db.SaveChangesAsync(cancellationToken);
    }

    private static void ValidateRequest(AttributeRequest request)
    {
        if (string.IsNullOrWhiteSpace(request.Name) || request.Name.Trim().Length > 160)
            throw new ValidationException("Name is required and must be at most 160 characters.");

        if (!Categories.Contains(request.Category))
            throw new ValidationException("Invalid category.");

        if (!Enum.IsDefined(request.Type))
            throw new ValidationException("Invalid type.");

        if (request.Description?.Length > 2000)
            throw new ValidationException("Description is too long.");

        if (request.Type == AttributeType.Dropdown && (request.Options.Count == 0 || request.Options.Any(option => string.IsNullOrWhiteSpace(option) || option.Length > 200)
            || request.Options.Distinct(StringComparer.OrdinalIgnoreCase).Count() != request.Options.Count))
            throw new ValidationException("Dropdown options must be unique and nonempty.");
    }

    private static AttributeDetail ToDetail(AttributeDefinition item) => new(
        item.Id, item.Name, item.Description, item.Category, item.Type, item.IsBuiltIn, item.Version,
        item.Options.OrderBy(option => option.SortOrder).Select(option => new AttributeOptionView(option.Id, option.Value)).ToList());
}
