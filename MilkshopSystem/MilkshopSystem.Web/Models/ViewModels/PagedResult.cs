namespace MilkshopSystem.Web.Models.ViewModels
{
    /// <summary>
    /// Generic wrapper used by every list page (Customer, Product, Stock, Invoice, etc.)
    /// so pagination + search stays identical across all modules.
    /// </summary>
    public class PagedResult<T>
    {
        public List<T> Items { get; set; } = new();
        public int TotalRecords { get; set; }
        public int PageNumber { get; set; } = 1;
        public int PageSize { get; set; } = 10;
        public string? SearchTerm { get; set; }

        public int TotalPages => (int)Math.Ceiling(TotalRecords / (double)PageSize);
        public bool HasPreviousPage => PageNumber > 1;
        public bool HasNextPage => PageNumber < TotalPages;
    }

    /// <summary>
    /// Common query params every Index() list action accepts.
    /// e.g. /Customer?searchTerm=raj&pageNumber=2&pageSize=10
    /// </summary>
    public class ListQueryParams
    {
        public string? SearchTerm { get; set; }
        public int PageNumber { get; set; } = 1;
        public int PageSize { get; set; } = 10;
    }
}
