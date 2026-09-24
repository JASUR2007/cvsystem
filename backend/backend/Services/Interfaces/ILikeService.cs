namespace backend.Services.Interfaces;

public interface ILikeService {
    Task<object> GetLikesAsync(Guid cvId, CancellationToken cancellationToken = default);
    Task<object> AddLikeAsync(Guid cvId, CancellationToken cancellationToken = default);
    Task RemoveLikeAsync(Guid cvId, CancellationToken cancellationToken = default);
}
