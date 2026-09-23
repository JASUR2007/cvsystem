using backend.Api;
using backend.Auth;
using backend.Data;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace backend.Controllers;

[ApiController]
[Authorize(Roles = Roles.Recruiter + "," + Roles.Administrator)]
[Route("api/positions/{positionId:guid}/cvs")]
public sealed class PositionCvsController(AppDbContext db) : ControllerBase
{
    [HttpGet]
    public async Task<IActionResult> List(Guid positionId, string? q, Guid? attributeId, AccessOperator? operation, string? value,
        string? sort = null, int page = 1, int pageSize = 20, CancellationToken cancellationToken = default)
    {
        var position = await db.Positions.AsNoTracking().Include(item => item.Attributes).ThenInclude(link => link.Attribute)
            .FirstOrDefaultAsync(item => item.Id == positionId, cancellationToken);
        if (position is null) return NotFound();

        var query = db.Cvs.AsNoTracking().Where(cv => cv.PositionId == positionId && cv.Status == CvStatus.Published);
        if (!ApiModels.IsAdmin(User)) query = PositionAccess.EligibleCvs(query, db);
        if (!string.IsNullOrWhiteSpace(q)) query = query.Where(cv => EF.Functions.ILike(cv.Candidate.FirstName + " " + cv.Candidate.LastName, "%" + q.Trim() + "%"));
        if (attributeId is not null && operation is not null && value is not null)
        {
            var attribute = position.Attributes.FirstOrDefault(link => link.AttributeId == attributeId)?.Attribute;
            if (attribute is null) return BadRequest(new { message = "Attribute is not part of this position." });
            var error = PositionAccess.ValidateRule(attribute, operation.Value, value);
            if (error is not null) return BadRequest(new { message = error });
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

        query = sort switch
        {
            "likes" => query.OrderByDescending(cv => cv.Likes.Count).ThenByDescending(cv => cv.UpdatedAt),
            "candidate" => query.OrderBy(cv => cv.Candidate.LastName).ThenBy(cv => cv.Candidate.FirstName),
            _ => query.OrderByDescending(cv => cv.UpdatedAt)
        };

        var currentPage = ApiModels.Page(page);
        var size = ApiModels.PageSize(pageSize);
        var total = await query.CountAsync(cancellationToken);
        var cvs = await query.Skip((currentPage - 1) * size).Take(size)
            .Select(cv => new { cv.Id, cv.CandidateId, cv.Candidate.FirstName, cv.Candidate.LastName, cv.Candidate.Location,
                cv.Candidate.PhotoObjectKey, cv.UpdatedAt, Likes = cv.Likes.Count }).ToListAsync(cancellationToken);
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
        return Ok(new PositionCvPage(columns, items, currentPage, size, total));
    }

    private static string? DisplayValue(PositionAttributeView column, UserAttributeValue? value, Dictionary<Guid, string> optionMap, string firstName, string lastName, string? location, string? photoKey)
    {
        if (column.Name == "First Name") return firstName;
        if (column.Name == "Last Name") return lastName;
        if (column.Name == "Location") return location;
        if (column.Name == "Personal Photo") return photoKey is null ? null : "Photo";
        return column.Type switch
        {
            AttributeType.String or AttributeType.Text => value?.TextValue,
            AttributeType.Numeric => value?.NumberValue?.ToString(System.Globalization.CultureInfo.InvariantCulture),
            AttributeType.Date => value?.DateValue?.ToString(),
            AttributeType.Period => value?.PeriodStart is null ? null : value.PeriodStart + " – " + value.PeriodEnd,
            AttributeType.Boolean => value?.BooleanValue is null ? null : value.BooleanValue.Value ? "Yes" : "No",
            AttributeType.Dropdown => value?.SelectedOptionId is Guid optionId ? optionMap.GetValueOrDefault(optionId) : null,
            AttributeType.Image => value?.ImageObjectKey is null ? null : "Image",
            _ => null
        };
    }
}

public sealed record PositionCvCell(Guid AttributeId, string? Value);
public sealed record PositionCvRow(Guid Id, Guid CandidateId, string Candidate, DateTime UpdatedAt, int Likes, List<PositionCvCell> Values);
public sealed record PositionCvPage(List<PositionAttributeView> Columns, List<PositionCvRow> Items, int Page, int PageSize, int TotalItems);
