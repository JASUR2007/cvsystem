using backend.Api;
using backend.Auth;
using backend.Data;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace backend.Controllers;

[ApiController]
[Authorize(Roles = Roles.Candidate + "," + Roles.Administrator)]
[Route("api/projects")]
public sealed class ProjectsController(AppDbContext db) : ControllerBase
{
    [HttpGet]
    public async Task<IActionResult> List(Guid? userId, CancellationToken cancellationToken)
    {
        var id = ResolveUserId(userId);
        if (id is null) return Forbid();
        var projects = await db.Projects.AsNoTracking().Where(project => project.UserId == id)
            .Include(project => project.Tags).ThenInclude(link => link.Tag)
            .OrderByDescending(project => project.StartedOn).ToListAsync(cancellationToken);
        return Ok(projects.Select(View));
    }

    [HttpGet("{id:guid}")]
    public async Task<IActionResult> Get(Guid id, CancellationToken cancellationToken)
    {
        var project = await OwnProject(id, cancellationToken);
        return project is null ? NotFound() : Ok(View(project));
    }

    [HttpPost]
    public async Task<IActionResult> Create(ProjectRequest request, Guid? userId, CancellationToken cancellationToken)
    {
        var id = ResolveUserId(userId);
        if (id is null) return Forbid();
        var error = Validate(request);
        if (error is not null) return BadRequest(new { message = error });

        var project = new Project
        {
            Id = Guid.NewGuid(), UserId = id.Value, Name = request.Name.Trim(),
            StartedOn = request.StartedOn, EndedOn = request.EndedOn,
            Description = request.Description?.Trim() ?? string.Empty
        };
        db.Projects.Add(project);
        await SetTags(project, request.Tags, cancellationToken);
        await db.SaveChangesAsync(cancellationToken);
        return CreatedAtAction(nameof(Get), new { id = project.Id }, View(project));
    }

    [HttpPut("{id:guid}")]
    public async Task<IActionResult> Update(Guid id, ProjectRequest request, CancellationToken cancellationToken)
    {
        var project = await OwnProject(id, cancellationToken);
        if (project is null) return NotFound();
        if (project.Version != request.Version) return Conflict(new { message = "Project was changed in another session." });
        var error = Validate(request);
        if (error is not null) return BadRequest(new { message = error });

        project.Name = request.Name.Trim();
        project.StartedOn = request.StartedOn;
        project.EndedOn = request.EndedOn;
        project.Description = request.Description?.Trim() ?? string.Empty;
        project.Version++;
        await SetTags(project, request.Tags, cancellationToken);
        try
        {
            await db.SaveChangesAsync(cancellationToken);
        }
        catch (DbUpdateConcurrencyException)
        {
            return Conflict(new { message = "Project was changed in another session." });
        }

        return Ok(View(project));
    }

    [HttpDelete("{id:guid}")]
    public async Task<IActionResult> Delete(Guid id, CancellationToken cancellationToken)
    {
        var project = await OwnProject(id, cancellationToken);
        if (project is null) return NotFound();
        db.Projects.Remove(project);
        await db.SaveChangesAsync(cancellationToken);
        return NoContent();
    }

    private async Task<Project?> OwnProject(Guid id, CancellationToken cancellationToken)
    {
        var query = db.Projects.Include(project => project.Tags).ThenInclude(link => link.Tag).Where(project => project.Id == id);
        if (!ApiModels.IsAdmin(User)) query = query.Where(project => project.UserId == ApiModels.UserId(User));
        return await query.FirstOrDefaultAsync(cancellationToken);
    }

    private Guid? ResolveUserId(Guid? target)
    {
        var current = ApiModels.UserId(User);
        return target is null || target == current ? current : ApiModels.IsAdmin(User) ? target : null;
    }

    private async Task SetTags(Project project, List<string> names, CancellationToken cancellationToken)
    {
        var distinct = names.Select(name => name.Trim()).Distinct(StringComparer.OrdinalIgnoreCase).ToList();
        var removed = project.Tags.Where(link => !distinct.Contains(link.Tag.Name, StringComparer.OrdinalIgnoreCase)).ToList();
        db.ProjectTags.RemoveRange(removed);
        foreach (var link in removed) project.Tags.Remove(link);
        var missing = distinct.Where(name => project.Tags.All(link => !string.Equals(link.Tag.Name, name, StringComparison.OrdinalIgnoreCase))).ToList();
        var existing = await db.Tags.Where(tag => missing.Contains(tag.Name)).ToListAsync(cancellationToken);
        var tags = existing.Concat(missing.Where(name => existing.All(tag => !string.Equals(tag.Name, name, StringComparison.OrdinalIgnoreCase)))
            .Select(name => new Tag { Id = Guid.NewGuid(), Name = name })).ToList();
        foreach (var tag in tags)
        {
            project.Tags.Add(new ProjectTag { Project = project, Tag = tag, TagId = tag.Id });
        }
    }

    private static string? Validate(ProjectRequest request)
    {
        if (string.IsNullOrWhiteSpace(request.Name) || request.Name.Length > 200) return "Project name is required and must be at most 200 characters.";
        if (request.EndedOn < request.StartedOn) return "End date must follow start date.";
        if (request.Description?.Length > 10000) return "Description is too long.";
        if (request.Tags.Count > 20 || request.Tags.Any(tag => string.IsNullOrWhiteSpace(tag) || tag.Length > 80)) return "Invalid tags.";
        return null;
    }

    private static ProjectView View(Project project) => new(project.Id, project.Name, project.StartedOn, project.EndedOn,
        project.Description, project.Tags.Select(link => link.Tag.Name).Order().ToList(), project.Version);
}

public sealed record ProjectRequest(string Name, DateOnly StartedOn, DateOnly? EndedOn, string? Description, List<string> Tags, int Version);
public sealed record ProjectView(Guid Id, string Name, DateOnly StartedOn, DateOnly? EndedOn, string Description, List<string> Tags, int Version);
