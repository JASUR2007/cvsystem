using backend.Auth;
using backend.Common.Enums;
using backend.Common.Exceptions;
using backend.Common.Pagination;
using backend.Data;
using backend.DTOs.Cvs;
using backend.DTOs.Positions;
using backend.Entities;
using backend.Services.Interfaces;
using Microsoft.EntityFrameworkCore;

namespace backend.Services;

public class PositionService(AppDbContext db, ICurrentUserService currentUser) : IPositionService {
    public async Task<PagedResult<PositionListItem>> ListPositionsAsync(
        PositionLevel? level, string? q, bool? isPublic, int page, int pageSize,
        CancellationToken cancellationToken = default) {
        var query = db.Positions.AsNoTracking().AsQueryable();

        if (!currentUser.IsAuthenticated)
            query = query.Where(position => position.IsPublic);
        else if (currentUser.IsCandidate && !currentUser.IsAdmin)
            query = PositionAccessHelper.Eligible(query, db, currentUser.RequireUserId());

        if (level is not null) query = query.Where(position => position.Level == level);
        if (isPublic is not null) query = query.Where(position => position.IsPublic == isPublic.Value);
        if (!string.IsNullOrWhiteSpace(q)) query = query.Where(position => position.SearchVector.Matches(q.Trim()));

        var currentPage = Math.Max(1, page);
        var size = Math.Clamp(pageSize, 1, 100);
        var total = await query.CountAsync(cancellationToken);

        var items = await query.OrderByDescending(position => position.UpdatedAt).Skip((currentPage - 1) * size).Take(size)
            .Select(position => new PositionListItem(position.Id, position.Title, position.Company, position.Level,
                position.UpdatedAt, position.IsPublic, position.Cvs.Count(cv => cv.Status == CvStatus.Published)))
            .ToListAsync(cancellationToken);

        return new PagedResult<PositionListItem>(items, currentPage, size, total);
    }

    public async Task<PositionDetail> GetPositionByIdAsync(Guid id, CancellationToken cancellationToken = default) {
        var query = db.Positions.AsNoTracking().Where(item => item.Id == id);
        if (!currentUser.IsAuthenticated)
            query = query.Where(position => position.IsPublic);
        else if (currentUser.IsCandidate && !currentUser.IsAdmin)
            query = PositionAccessHelper.Eligible(query, db, currentUser.RequireUserId());

        var position = await query
            .Include(item => item.Attributes).ThenInclude(link => link.Attribute)
            .Include(item => item.AccessRules).ThenInclude(link => link.Attribute)
            .Include(item => item.ProjectTags).ThenInclude(link => link.Tag)
            .FirstOrDefaultAsync(cancellationToken);

        if (position is null)
            throw new NotFoundException("Position not found.");

        return ToDetail(position);
    }

    public async Task<PositionDetail> CreatePositionAsync(PositionRequest request, CancellationToken cancellationToken = default) {
        ValidateRequest(request);

        var position = new Position {
            Id = Guid.NewGuid(),
            Title = request.Title.Trim(),
            ShortDescription = request.ShortDescription.Trim(),
            Company = request.Company?.Trim(),
            Level = request.Level,
            IsPublic = request.IsPublic,
            MaxProjects = request.MaxProjects
        };

        await ApplyAttributesAndRulesAsync(position, request, cancellationToken);
        db.Positions.Add(position);
        await db.SaveChangesAsync(cancellationToken);

        return await GetPositionByIdAsync(position.Id, cancellationToken);
    }

    public async Task<PositionDetail> UpdatePositionAsync(Guid id, PositionRequest request, CancellationToken cancellationToken = default) {
        ValidateRequest(request);

        var position = await db.Positions
            .Include(item => item.Attributes)
            .Include(item => item.AccessRules)
            .Include(item => item.ProjectTags)
            .FirstOrDefaultAsync(item => item.Id == id, cancellationToken);

        if (position is null)
            throw new NotFoundException("Position not found.");

        if (position.Version != request.Version)
            throw new ConflictException("Position was changed in another session.");

        position.Title = request.Title.Trim();
        position.ShortDescription = request.ShortDescription.Trim();
        position.Company = request.Company?.Trim();
        position.Level = request.Level;
        position.IsPublic = request.IsPublic;
        position.MaxProjects = request.MaxProjects;
        position.UpdatedAt = DateTime.UtcNow;
        position.Version++;

        db.PositionAttributes.RemoveRange(position.Attributes);
        db.PositionAccessRules.RemoveRange(position.AccessRules);
        db.PositionProjectTags.RemoveRange(position.ProjectTags);

        await ApplyAttributesAndRulesAsync(position, request, cancellationToken);
        await db.SaveChangesAsync(cancellationToken);

        return await GetPositionByIdAsync(position.Id, cancellationToken);
    }

    public async Task DeletePositionAsync(Guid id, CancellationToken cancellationToken = default) {
        var position = await db.Positions.FindAsync([id], cancellationToken);
        if (position is null)
            throw new NotFoundException("Position not found.");

        if (await db.Cvs.AnyAsync(cv => cv.PositionId == id, cancellationToken))
            throw new ConflictException("Position has published or draft CVs.");

        db.Positions.Remove(position);
        await db.SaveChangesAsync(cancellationToken);
    }

    public async Task<PositionDetail> DuplicatePositionAsync(Guid id, CancellationToken cancellationToken = default) {
        var source = await db.Positions.AsNoTracking()
            .Include(item => item.Attributes)
            .Include(item => item.AccessRules)
            .Include(item => item.ProjectTags)
            .FirstOrDefaultAsync(item => item.Id == id, cancellationToken);

        if (source is null)
            throw new NotFoundException("Position not found.");

        var duplicate = new Position {
            Id = Guid.NewGuid(),
            Title = $"{source.Title} (Copy)",
            ShortDescription = source.ShortDescription,
            Company = source.Company,
            Level = source.Level,
            IsPublic = source.IsPublic,
            MaxProjects = source.MaxProjects,
            Attributes = source.Attributes.Select(attribute => new PositionAttribute {
                AttributeId = attribute.AttributeId,
                SortOrder = attribute.SortOrder,
                IsRequired = attribute.IsRequired
            }).ToList(),
            AccessRules = source.AccessRules.Select(rule => new PositionAccessRule {
                Id = Guid.NewGuid(),
                AttributeId = rule.AttributeId,
                Operator = rule.Operator,
                ComparisonValue = rule.ComparisonValue,
                NumberValue = rule.NumberValue,
                BooleanValue = rule.BooleanValue,
                OptionId = rule.OptionId
            }).ToList(),
            ProjectTags = source.ProjectTags.Select(tag => new PositionProjectTag { TagId = tag.TagId }).ToList()
        };

        db.Positions.Add(duplicate);
        await db.SaveChangesAsync(cancellationToken);

        return await GetPositionByIdAsync(duplicate.Id, cancellationToken);
    }

    public async Task<PositionCvPage> ListPositionCvsAsync(
        Guid positionId, string? q, Guid? attributeId, AccessOperator? operation, string? value,
        string? sort, int page, int pageSize, CancellationToken cancellationToken = default) {
        var position = await db.Positions.AsNoTracking().Include(item => item.Attributes).ThenInclude(link => link.Attribute)
            .FirstOrDefaultAsync(item => item.Id == positionId, cancellationToken);
        if (position is null)
            throw new NotFoundException("Position not found.");

        var query = db.Cvs.AsNoTracking().Where(cv => cv.PositionId == positionId && cv.Status == CvStatus.Published);
        if (!currentUser.IsAdmin) query = PositionAccessHelper.EligibleCvs(query, db);

        if (!string.IsNullOrWhiteSpace(q))
            query = query.Where(cv => EF.Functions.ILike(cv.Candidate.FirstName + " " + cv.Candidate.LastName, "%" + q.Trim() + "%"));

        if (attributeId is not null && operation is not null && value is not null) {
            var attribute = position.Attributes.FirstOrDefault(link => link.AttributeId == attributeId)?.Attribute;
            if (attribute is null)
                throw new ValidationException("Attribute is not part of this position.");

            var error = PositionAccessHelper.ValidateRule(attribute, operation.Value, value);
            if (error is not null)
                throw new ValidationException(error);

            var number = attribute.Type == AttributeType.Numeric ? decimal.Parse(value, System.Globalization.CultureInfo.InvariantCulture) : (decimal?)null;
            var boolean = attribute.Type == AttributeType.Boolean ? bool.Parse(value) : (bool?)null;
            var option = attribute.Type == AttributeType.Dropdown ? Guid.Parse(value) : (Guid?)null;

            query = query.Where(cv => db.UserAttributeValues.Any(item => item.UserId == cv.CandidateId && item.AttributeId == attributeId &&
                ((attribute.Type == AttributeType.Numeric &&
                    ((operation == AccessOperator.Equals && item.NumberValue == number) ||
                     (operation == AccessOperator.GreaterThan && item.NumberValue > number) ||
                     (operation == AccessOperator.GreaterThanOrEqual && item.NumberValue >= number) ||
                     (operation == AccessOperator.LessThan && item.NumberValue < number) ||
                     (operation == AccessOperator.LessThanOrEqual && item.NumberValue <= number))) ||
                 (attribute.Type == AttributeType.Boolean && item.BooleanValue == boolean) ||
                 (attribute.Type == AttributeType.Dropdown && item.SelectedOptionId == option) ||
                 ((attribute.Type == AttributeType.String || attribute.Type == AttributeType.Text) &&
                    ((operation == AccessOperator.Equals && item.TextValue == value) ||
                     (operation == AccessOperator.Contains && item.TextValue != null && EF.Functions.ILike(item.TextValue, "%" + value + "%")))))));
        }

        query = sort switch {
            "likes" => query.OrderByDescending(cv => cv.Likes.Count).ThenByDescending(cv => cv.UpdatedAt),
            "candidate" => query.OrderBy(cv => cv.Candidate.LastName).ThenBy(cv => cv.Candidate.FirstName),
            _ => query.OrderByDescending(cv => cv.UpdatedAt)
        };

        var currentPage = Math.Max(1, page);
        var size = Math.Clamp(pageSize, 1, 100);
        var total = await query.CountAsync(cancellationToken);

        var cvs = await query.Skip((currentPage - 1) * size).Take(size)
            .Select(cv => new {
                cv.Id,
                cv.CandidateId,
                cv.Candidate.FirstName,
                cv.Candidate.LastName,
                cv.Candidate.Location,
                cv.Candidate.PhotoObjectKey,
                cv.UpdatedAt,
                Likes = cv.Likes.Count
            }).ToListAsync(cancellationToken);

        var candidateIds = cvs.Select(cv => cv.CandidateId).ToList();
        var attributeIds = position.Attributes.Select(link => link.AttributeId).ToList();
        var values = await db.UserAttributeValues.AsNoTracking()
            .Where(item => candidateIds.Contains(item.UserId) && attributeIds.Contains(item.AttributeId))
            .ToListAsync(cancellationToken);

        var valueMap = values.ToDictionary(item => (item.UserId, item.AttributeId));
        var selectedOptionIds = values.Where(item => item.SelectedOptionId is not null).Select(item => item.SelectedOptionId!.Value).Distinct().ToList();
        var optionMap = await db.AttributeOptions.AsNoTracking().Where(item => selectedOptionIds.Contains(item.Id))
            .ToDictionaryAsync(item => item.Id, item => item.Value, cancellationToken);

        var columns = position.Attributes.OrderBy(link => link.SortOrder)
            .Select(link => new PositionAttributeView(link.AttributeId, link.Attribute.Name, link.Attribute.Type, link.SortOrder, link.IsRequired)).ToList();

        var items = cvs.Select(cv => new PositionCvRow(cv.Id, cv.CandidateId, cv.FirstName + " " + cv.LastName, cv.UpdatedAt, cv.Likes,
            columns.Select(column => new PositionCvCell(column.AttributeId, DisplayValue(column, valueMap.GetValueOrDefault((cv.CandidateId, column.AttributeId)), optionMap, cv.FirstName, cv.LastName, cv.Location, cv.PhotoObjectKey))).ToList())).ToList();

        return new PositionCvPage(columns, items, currentPage, size, total);
    }

    private static void ValidateRequest(PositionRequest request) {
        if (string.IsNullOrWhiteSpace(request.Title) || request.Title.Length > 200)
            throw new ValidationException("Title is required and must be at most 200 characters.");

        if (string.IsNullOrWhiteSpace(request.ShortDescription) || request.ShortDescription.Length > 2000)
            throw new ValidationException("Short description is required and must be at most 2000 characters.");

        if (request.Company?.Length > 160)
            throw new ValidationException("Company must be at most 160 characters.");

        if (request.Level is not null && !Enum.IsDefined(request.Level.Value))
            throw new ValidationException("Invalid level.");

        if (request.MaxProjects is < 0 or > 10)
            throw new ValidationException("Max projects must be between 0 and 10.");

        if (request.ProjectTags.Count > 10 || request.ProjectTags.Any(tag => string.IsNullOrWhiteSpace(tag) || tag.Length > 80))
            throw new ValidationException("Invalid project tags.");
    }

    private async Task ApplyAttributesAndRulesAsync(Position position, PositionRequest request, CancellationToken cancellationToken) {
        var attributeIds = request.Attributes.Select(attribute => attribute.AttributeId)
            .Concat(request.AccessRules.Select(rule => rule.AttributeId)).Distinct().ToList();
        var attributes = await db.Attributes.Include(attribute => attribute.Options)
            .Where(attribute => attributeIds.Contains(attribute.Id)).ToDictionaryAsync(attribute => attribute.Id, cancellationToken);

        if (request.Attributes.Any(attribute => !attributes.ContainsKey(attribute.AttributeId)))
            throw new ValidationException("One or more attributes were not found.");

        if (request.Attributes.Select(attribute => attribute.AttributeId).Distinct().Count() != request.Attributes.Count)
            throw new ValidationException("Attributes must be unique.");

        position.Attributes = request.Attributes.Select(attribute => new PositionAttribute {
            Position = position,
            AttributeId = attribute.AttributeId,
            SortOrder = attribute.SortOrder,
            IsRequired = attribute.IsRequired
        }).ToList();

        position.AccessRules = [];
        foreach (var rule in request.AccessRules) {
            if (!attributes.TryGetValue(rule.AttributeId, out var attribute))
                throw new ValidationException("Access rule attribute was not found.");

            var error = PositionAccessHelper.ValidateRule(attribute, rule.Operator, rule.ComparisonValue);
            if (error is not null) throw new ValidationException(error);

            position.AccessRules.Add(new PositionAccessRule {
                Position = position,
                AttributeId = rule.AttributeId,
                Operator = rule.Operator,
                ComparisonValue = rule.ComparisonValue.Trim(),
                NumberValue = attribute.Type == AttributeType.Numeric ? decimal.Parse(rule.ComparisonValue, System.Globalization.CultureInfo.InvariantCulture) : null,
                BooleanValue = attribute.Type == AttributeType.Boolean ? bool.Parse(rule.ComparisonValue) : null,
                OptionId = attribute.Type == AttributeType.Dropdown ? Guid.Parse(rule.ComparisonValue) : null
            });
        }

        var tagNames = request.ProjectTags.Select(tag => tag.Trim()).Distinct(StringComparer.OrdinalIgnoreCase).ToList();
        var existingTags = await db.Tags.Where(tag => tagNames.Contains(tag.Name)).ToListAsync(cancellationToken);
        var tags = existingTags.Concat(tagNames.Where(name => existingTags.All(tag => !string.Equals(tag.Name, name, StringComparison.OrdinalIgnoreCase)))
            .Select(name => new Tag { Id = Guid.NewGuid(), Name = name })).ToList();

        position.ProjectTags = tags.Select(tag => new PositionProjectTag { Position = position, Tag = tag, TagId = tag.Id }).ToList();
    }

    private static PositionDetail ToDetail(Position position) => new(
        position.Id, position.Title, position.ShortDescription, position.Company, position.Level, position.IsPublic,
        position.MaxProjects, position.Version, position.CreatedAt, position.UpdatedAt,
        position.Attributes.OrderBy(attribute => attribute.SortOrder).Select(attribute => new PositionAttributeView(
            attribute.AttributeId, attribute.Attribute.Name, attribute.Attribute.Type, attribute.SortOrder, attribute.IsRequired)).ToList(),
        position.AccessRules.Select(rule => new AccessRuleView(
            rule.Id, rule.AttributeId, rule.Attribute.Name, rule.Operator, rule.ComparisonValue)).ToList(),
        position.ProjectTags.Select(link => link.Tag.Name).Order().ToList());

    private static string? DisplayValue(PositionAttributeView column, UserAttributeValue? value, Dictionary<Guid, string> optionMap, string firstName, string lastName, string? location, string? photoKey) {
        if (column.Name == "First Name") return firstName;
        if (column.Name == "Last Name") return lastName;
        if (column.Name == "Location") return location;
        if (column.Name == "Personal Photo") return photoKey;
        return column.Type switch {
            AttributeType.String or AttributeType.Text => value?.TextValue,
            AttributeType.Numeric => value?.NumberValue?.ToString(System.Globalization.CultureInfo.InvariantCulture),
            AttributeType.Date => value?.DateValue?.ToString(),
            AttributeType.Period => value?.PeriodStart is null ? null : value.PeriodStart + " – " + value.PeriodEnd,
            AttributeType.Boolean => value?.BooleanValue is null ? null : value.BooleanValue.Value ? "Yes" : "No",
            AttributeType.Dropdown => value?.SelectedOptionId is Guid optionId ? optionMap.GetValueOrDefault(optionId) : null,
            AttributeType.Image => value?.ImageObjectKey,
            _ => null
        };
    }
}
