using backend.Common.Enums;
using backend.Common.Pagination;
using backend.DTOs.Attributes;

namespace backend.Services.Interfaces;

public interface IAttributeService
{
    Task<PagedResult<AttributeListItem>> ListAsync(
        string? prefix, string? category, AttributeType? type, bool? recent,
        int page, int pageSize, CancellationToken cancellationToken = default);

    Task<AttributeDetail> GetByIdAsync(Guid id, CancellationToken cancellationToken = default);
    Task<AttributeDetail> CreateAsync(AttributeRequest request, CancellationToken cancellationToken = default);
    Task<AttributeDetail> UpdateAsync(Guid id, AttributeRequest request, CancellationToken cancellationToken = default);
    Task DeleteAsync(Guid id, CancellationToken cancellationToken = default);
}
