using backend.DTOs.Discussions;

namespace backend.Services.Interfaces;

public interface IDiscussionService
{
    Task<List<DiscussionView>> ListPostsAsync(Guid positionId, CancellationToken cancellationToken = default);
    Task<DiscussionView> AddPostAsync(Guid positionId, DiscussionRequest request, CancellationToken cancellationToken = default);
    Task<DiscussionView> UpdatePostAsync(Guid id, DiscussionRequest request, CancellationToken cancellationToken = default);
    Task DeletePostAsync(Guid id, CancellationToken cancellationToken = default);
}
