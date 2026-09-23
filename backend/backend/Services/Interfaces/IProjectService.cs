using backend.DTOs.Projects;

namespace backend.Services.Interfaces;

public interface IProjectService
{
    Task<List<ProjectView>> ListUserProjectsAsync(Guid? targetUserId, CancellationToken cancellationToken = default);
    Task<ProjectView> CreateProjectAsync(Guid? targetUserId, ProjectRequest request, CancellationToken cancellationToken = default);
    Task<ProjectView> UpdateProjectAsync(Guid id, ProjectRequest request, CancellationToken cancellationToken = default);
    Task DeleteProjectAsync(Guid id, CancellationToken cancellationToken = default);
}
