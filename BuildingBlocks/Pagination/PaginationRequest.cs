namespace BuildingBlocks.Pagination;

public sealed record PaginationRequest(int Page, int PageSize)
{
    public const int DefaultPage = 1;
    public const int DefaultPageSize = 20;
    public const int MaxPageSize = 100;

    public int Skip => (Page - 1) * PageSize;

    public static PaginationRequest Normalize(int? page, int? pageSize)
    {
        int resolvedPage = page.GetValueOrDefault(DefaultPage);
        if (resolvedPage < DefaultPage) resolvedPage = DefaultPage;

        int resolvedSize = pageSize.GetValueOrDefault(DefaultPageSize);
        if (resolvedSize < 1) resolvedSize = DefaultPageSize;
        if (resolvedSize > MaxPageSize) resolvedSize = MaxPageSize;

        return new PaginationRequest(resolvedPage, resolvedSize);
    }
}
