using backend.Auth;
using backend.Common.Enums;
using backend.Common.Pagination;
using backend.DTOs.Attributes;
using backend.Services.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace backend.Controllers;

[ApiController]
[Authorize]
[Route("api/attributes")]
public sealed class AttributesController(IAttributeService attributeService) : ControllerBase
{
    [HttpGet]
    public async Task<ActionResult<PagedResult<AttributeListItem>>> List(
        string? prefix, string? category, AttributeType? type, bool? recent,
        int page = 1, int pageSize = 20, CancellationToken cancellationToken = default)
    {
        var result = await attributeService.ListAsync(prefix, category, type, recent, page, pageSize, cancellationToken);
        return Ok(result);
    }

    [HttpGet("recent")]
    public Task<ActionResult<PagedResult<AttributeListItem>>> Recent(CancellationToken cancellationToken) =>
        List(null, null, null, true, 1, 10, cancellationToken);

    [HttpGet("categories")]
    [HttpGet("/api/attribute-categories")]
    public async Task<ActionResult<List<string>>> Categories(CancellationToken cancellationToken)
    {
        var result = await attributeService.GetCategoriesAsync(cancellationToken);
        return Ok(result);
    }

    [HttpGet("{id:guid}")]
    public async Task<ActionResult<AttributeDetail>> Get(Guid id, CancellationToken cancellationToken)
    {
        var result = await attributeService.GetByIdAsync(id, cancellationToken);
        return Ok(result);
    }

    [Authorize(Roles = Roles.Recruiter + "," + Roles.Administrator)]
    [HttpPost]
    public async Task<ActionResult<AttributeDetail>> Create(AttributeRequest request, CancellationToken cancellationToken)
    {
        var result = await attributeService.CreateAsync(request, cancellationToken);
        return CreatedAtAction(nameof(Get), new { id = result.Id }, result);
    }

    [Authorize(Roles = Roles.Recruiter + "," + Roles.Administrator)]
    [HttpPut("{id:guid}")]
    public async Task<ActionResult<AttributeDetail>> Update(Guid id, AttributeRequest request, CancellationToken cancellationToken)
    {
        var result = await attributeService.UpdateAsync(id, request, cancellationToken);
        return Ok(result);
    }

    [Authorize(Roles = Roles.Recruiter + "," + Roles.Administrator)]
    [HttpDelete("{id:guid}")]
    public async Task<IActionResult> Delete(Guid id, CancellationToken cancellationToken)
    {
        await attributeService.DeleteAsync(id, cancellationToken);
        return NoContent();
    }
}
