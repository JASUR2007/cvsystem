using backend.Common.Exceptions;
using backend.DTOs.Integrations;
using backend.Services.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace backend.Controllers;

[ApiController]
[AllowAnonymous]
[Route("api/integrations/odoo")]
public sealed class OdooIntegrationController(IOdooIntegrationService odooIntegrationService) : ControllerBase {
    [HttpGet("position-results")]
    public async Task<ActionResult<PositionAggregatedResultView>> GetPositionResults(
        [FromHeader(Name = "X-API-Token")] string? apiToken,
        CancellationToken cancellationToken) {
        if (string.IsNullOrWhiteSpace(apiToken)) {
            return Unauthorized(new { message = "Missing X-API-Token header." });
        }

        try {
            var result = await odooIntegrationService.GetPositionResultsByTokenAsync(apiToken, cancellationToken);
            return Ok(result);
        }
        catch (UnauthorizedAccessException ex) {
            return Unauthorized(new { message = ex.Message });
        }
        catch (NotFoundException ex) {
            return NotFound(new { message = ex.Message });
        }
    }
}
