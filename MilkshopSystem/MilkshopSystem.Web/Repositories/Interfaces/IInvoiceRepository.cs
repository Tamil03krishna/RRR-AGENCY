using MilkshopSystem.Web.Models.Entities;
using MilkshopSystem.Web.Models.ViewModels;
namespace MilkshopSystem.Web.Repositories.Interfaces
{
    public interface IInvoiceRepository
    {
        Task<PagedResult<Invoice>> GetPagedAsync(string? search, int pageNumber, int pageSize);
        Task<Invoice?> GetByIdAsync(int id);
        Task<string> GetNextInvoiceNoAsync();
        Task<int> CreateInvoiceAsync(Invoice invoice); // transactional: insert + stock reduce + balance update
        Task<int> AddPaymentAsync(InvoicePayment payment); // pay off balance on a later visit
        Task<decimal> GetTodaySalesAsync();
        Task<decimal> GetMonthSalesAsync();
        Task<List<(int Month, decimal Total)>> GetMonthlyEarningsAsync(int year, int? productId = null);
        Task<List<(DateTime WeekStart, decimal Total)>> GetWeeklyEarningsAsync(DateTime from, DateTime to);
        Task<List<(int Year, decimal Total)>> GetYearlyEarningsAsync();
    }
}
