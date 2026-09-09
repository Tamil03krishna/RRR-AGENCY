using MilkshopSystem.Web.Models.Entities;
using MilkshopSystem.Web.Models.ViewModels;
namespace MilkshopSystem.Web.Repositories.Interfaces
{
    public interface IInvoiceRepository
    {
        Task<PagedResult<Invoice>> GetPagedAsync(string? search, int pageNumber, int pageSize);
        Task<Invoice?> GetByIdAsync(int id);
        Task<string> GetNextInvoiceNoAsync();
        Task<int> CreateInvoiceAsync(Invoice invoice); 
        Task<int> AddPaymentAsync(InvoicePayment payment);
        Task UpdateInvoiceItemAsync(int invoiceId, int itemId, decimal qty, decimal unitPrice, string priceType);
        Task DeleteInvoiceItemAsync(int invoiceId, int itemId);
        Task<decimal> GetTodaySalesAsync();
        Task<decimal> GetMonthSalesAsync();
        Task<List<(int Month, decimal Total)>> GetMonthlyEarningsAsync(int year, int? productId = null);
        Task<List<(DateTime WeekStart, decimal Total)>> GetWeeklyEarningsAsync(DateTime from, DateTime to);
        Task<List<(int Year, decimal Total)>> GetYearlyEarningsAsync();
        // New methods for profit calculation
        Task<List<MonthlyEarningDto>> GetMonthlyEarningsWithProfitAsync(int year, int? productId = null);
        Task<List<WeeklyEarningDto>> GetWeeklyEarningsWithProfitAsync(int weeks, int? productId = null);
        Task<List<YearlyEarningDto>> GetYearlyEarningsWithProfitAsync(int? productId = null);
        Task<List<ProductPerformanceDto>> GetProductPerformanceAsync(int year);
    }

    public class MonthlyEarningDto
    {
        public int Month { get; set; }
        public decimal Revenue { get; set; }
        public decimal Profit { get; set; }
        public int Count { get; set; }
    }

    public class WeeklyEarningDto
    {
        public string WeekStart { get; set; } = string.Empty;
        public decimal Revenue { get; set; }
        public decimal Profit { get; set; }
        public int Count { get; set; }
    }

    public class YearlyEarningDto
    {
        public int Year { get; set; }
        public decimal Revenue { get; set; }
        public decimal Profit { get; set; }
        public int Count { get; set; }
    }

    public class ProductPerformanceDto
    {
        public string ProductName { get; set; } = string.Empty;
        public decimal Revenue { get; set; }
        public decimal Profit { get; set; }
        public int Quantity { get; set; }
    }
}
