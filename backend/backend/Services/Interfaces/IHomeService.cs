using backend.DTOs.Home;
using backend.DTOs.Positions;

namespace backend.Services.Interfaces;

public interface IHomeService
{
    Task<HomeStatisticsResponse> GetStatisticsAsync(CancellationToken cancellationToken = default);
    Task<List<PositionListItem>> GetLatestPositionsAsync(CancellationToken cancellationToken = default);
    Task<List<PopularPositionResponse>> GetPopularPositionsAsync(CancellationToken cancellationToken = default);
    Task<List<TagResponse>> GetPopularTagsAsync(CancellationToken cancellationToken = default);
}
