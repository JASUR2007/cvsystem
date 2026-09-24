using backend.Auth;
using backend.Common.Exceptions;
using backend.Data;
using backend.DTOs.Projects;
using backend.Entities;
using backend.Services.Interfaces;
using Microsoft.EntityFrameworkCore;

namespace backend.Services;

public class ProjectService(AppDbContext db, ICurrentUserService currentUser) : IProjectService {
    public async Task<List<ProjectView>> ListUserProjectsAsync(Guid? targetUserId, CancellationToken cancellationToken = default) {
        var id = ResolveUserId(targetUserId);
        var projects = await db.Projects.AsNoTracking().Where(project => project.UserId == id)
            .Include(project => project.Tags).ThenInclude(link => link.Tag)
            .OrderByDescending(project => project.StartedOn)
            .ToListAsync(cancellationToken);

        return projects.Select(View).ToList();
    }

    public async Task<ProjectView> CreateProjectAsync(Guid? targetUserId, ProjectRequest request, CancellationToken cancellationToken = default) {
        Validate(request);
        var id = ResolveUserId(targetUserId);

        var project = new Project {
            Id = Guid.NewGuid(),
            UserId = id,
            Name = request.Name.Trim(),
            StartedOn = request.StartedOn,
            EndedOn = request.EndedOn,
            Description = request.Description?.Trim() ?? string.Empty
        };

        await SetTagsAsync(project, request.Tags, cancellationToken);
        db.Projects.Add(project);
        await db.SaveChangesAsync(cancellationToken);

        return View(project);
    }

    public async Task<ProjectView> UpdateProjectAsync(Guid id, ProjectRequest request, CancellationToken cancellationToken = default) {
        var project = await OwnProjectAsync(id, cancellationToken);
        if (project is null)
            throw new NotFoundException("Project not found.");

        if (project.Version != request.Version)
            throw new ConflictException("Project was changed in another session.");

        Validate(request);

        project.Name = request.Name.Trim();
        project.StartedOn = request.StartedOn;
        project.EndedOn = request.EndedOn;
        project.Description = request.Description?.Trim() ?? string.Empty;
        project.Version++;

        await SetTagsAsync(project, request.Tags, cancellationToken);
        await db.SaveChangesAsync(cancellationToken);

        return View(project);
    }

    public async Task DeleteProjectAsync(Guid id, CancellationToken cancellationToken = default) {
        var project = await OwnProjectAsync(id, cancellationToken);
        if (project is null)
            throw new NotFoundException("Project not found.");

        db.Projects.Remove(project);
        await db.SaveChangesAsync(cancellationToken);
    }

    private Guid ResolveUserId(Guid? target) {
        var current = currentUser.RequireUserId();
        if (target is null || target == current) return current;
        if (currentUser.IsAdmin) return target.Value;
        throw new ForbiddenException();
    }

    private async Task<Project?> OwnProjectAsync(Guid id, CancellationToken cancellationToken) {
        var query = db.Projects.Include(project => project.Tags).ThenInclude(link => link.Tag).Where(project => project.Id == id);
        if (!currentUser.IsAdmin) query = query.Where(project => project.UserId == currentUser.RequireUserId());
        return await query.FirstOrDefaultAsync(cancellationToken);
    }

    private async Task SetTagsAsync(Project project, List<string> names, CancellationToken cancellationToken) {
        var distinct = names.Select(name => name.Trim()).Distinct(StringComparer.OrdinalIgnoreCase).ToList();
        var removed = project.Tags.Where(link => !distinct.Contains(link.Tag.Name, StringComparer.OrdinalIgnoreCase)).ToList();

        db.ProjectTags.RemoveRange(removed);
        foreach (var link in removed) project.Tags.Remove(link);

        var missing = distinct.Where(name => project.Tags.All(link => !string.Equals(link.Tag.Name, name, StringComparison.OrdinalIgnoreCase))).ToList();
        var existing = await db.Tags.Where(tag => missing.Contains(tag.Name)).ToListAsync(cancellationToken);

        var tags = existing.Concat(missing.Where(name => existing.All(tag => !string.Equals(tag.Name, name, StringComparison.OrdinalIgnoreCase)))
            .Select(name => new Tag { Id = Guid.NewGuid(), Name = name })).ToList();

        foreach (var tag in tags) {
            project.Tags.Add(new ProjectTag { Project = project, Tag = tag, TagId = tag.Id });
        }
    }

    private static void Validate(ProjectRequest request) {
        if (string.IsNullOrWhiteSpace(request.Name) || request.Name.Length > 200)
            throw new ValidationException("Project name is required and must be at most 200 characters.");

        if (request.EndedOn < request.StartedOn)
            throw new ValidationException("End date must follow start date.");

        if (request.Description?.Length > 10000)
            throw new ValidationException("Description is too long.");

        if (request.Tags.Count > 20 || request.Tags.Any(tag => string.IsNullOrWhiteSpace(tag) || tag.Length > 80))
            throw new ValidationException("Invalid tags.");
    }

    private static ProjectView View(Project project) => new(
        project.Id,
        project.Name,
        project.StartedOn,
        project.EndedOn,
        project.Description,
        project.Tags.Select(link => link.Tag.Name).Order().ToList(),
        project.Version);
}
