using backend.Common.Enums;
using backend.Common.Pagination;
using backend.DTOs.Cvs;
using backend.DTOs.Positions;

namespace backend.Services.Interfaces;

public interface IPositionService
{
    Task<PagedResult<PositionListItem>> ListPositionsAsync(
        PositionLevel? level, string? q, bool? isPublic, int page, int pageSize,
        CancellationToken cancellationToken = default);

    Task<PositionDetail> GetPositionByIdAsync(Guid id, CancellationToken cancellationToken = default);
    Task<PositionDetail> CreatePositionAsync(PositionRequest request, CancellationToken cancellationToken = default);
    Task<PositionDetail> UpdatePositionAsync(Guid id, PositionRequest request, CancellationToken cancellationToken = default);
    Task DeletePositionAsync(Guid id, CancellationToken cancellationToken = default);
    Task<PositionDetail> DuplicatePositionAsync(Guid id, CancellationToken cancellationToken = default);

    Task<PositionCvPage> ListPositionCvsAsync(
        Guid positionId, string? q, Guid? attributeId, AccessOperator? operation, string? value,
        string? sort, int page, int pageSize, CancellationToken cancellationToken = default);
}
