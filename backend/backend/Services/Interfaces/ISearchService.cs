using backend.Common.Pagination;
using backend.DTOs.Positions;
using backend.DTOs.Search;

namespace backend.Services.Interfaces;

public interface ISearchService
{
    Task<object> SearchAllAsync(string q, CancellationToken cancellationToken = default);
    Task<PagedResult<PositionListItem>> SearchPositionsAsync(string q, int page, int pageSize, CancellationToken cancellationToken = default);
    Task<PagedResult<SearchCvItem>> SearchCvsAsync(string q, int page, int pageSize, CancellationToken cancellationToken = default);
}
