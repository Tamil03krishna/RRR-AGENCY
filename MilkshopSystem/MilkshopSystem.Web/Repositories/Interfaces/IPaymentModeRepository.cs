using MilkshopSystem.Web.Models.Entities;
using MilkshopSystem.Web.Models.ViewModels;
namespace MilkshopSystem.Web.Repositories.Interfaces
{
    public interface IPaymentModeRepository
    {
        Task<PagedResult<PaymentMode>> GetPagedAsync(string? search, int pageNumber, int pageSize);
        Task<List<PaymentMode>> GetAllActiveAsync();
        Task<PaymentMode?> GetByIdAsync(int id);
        Task<int> CreateAsync(PaymentMode mode);
        Task<bool> UpdateAsync(PaymentMode mode);
        Task<bool> DeleteAsync(int id);
    }
}
