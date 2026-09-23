using backend.DTOs.Projects;
using backend.Services.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace backend.Controllers;

[ApiController]
[Authorize]
public sealed class ProjectsController(IProjectService projectService) : ControllerBase
{
    [HttpGet("api/profile/projects")]
    [HttpGet("api/users/{userId:guid}/projects")]
    public async Task<ActionResult<List<ProjectView>>> List(Guid? userId, CancellationToken cancellationToken)
    {
        var result = await projectService.ListUserProjectsAsync(userId, cancellationToken);
        return Ok(result);
    }

    [HttpPost("api/profile/projects")]
    [HttpPost("api/users/{userId:guid}/projects")]
    public async Task<ActionResult<ProjectView>> Create(Guid? userId, ProjectRequest request, CancellationToken cancellationToken)
    {
        var result = await projectService.CreateProjectAsync(userId, request, cancellationToken);
        return CreatedAtAction(nameof(List), new { userId }, result);
    }

    [HttpPut("api/projects/{id:guid}")]
    public async Task<ActionResult<ProjectView>> Update(Guid id, ProjectRequest request, CancellationToken cancellationToken)
    {
        var result = await projectService.UpdateProjectAsync(id, request, cancellationToken);
        return Ok(result);
    }

    [HttpDelete("api/projects/{id:guid}")]
    public async Task<IActionResult> Delete(Guid id, CancellationToken cancellationToken)
    {
        await projectService.DeleteProjectAsync(id, cancellationToken);
        return NoContent();
    }
}
