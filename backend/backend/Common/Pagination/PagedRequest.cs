namespace backend.Common.Pagination;

public record PagedRequest(int Page = 1, int PageSize = 20)
{
    public int NormalizedPage => Math.Max(1, Page);
    public int NormalizedPageSize => Math.Clamp(PageSize, 1, 100);
    public int Skip => (NormalizedPage - 1) * NormalizedPageSize;
}
