using MilkshopSystem.Web.Models.Entities;
using MilkshopSystem.Web.Models.ViewModels;
namespace MilkshopSystem.Web.Repositories.Interfaces
{
    public interface ICustomerRepository
    {
        Task<PagedResult<Customer>> GetPagedAsync(string? search, int pageNumber, int pageSize);
        Task<Customer?> GetByIdAsync(int id);
        Task<List<Customer>> SearchAsync(string term, int maxResults = 10); // for billing autocomplete
        Task<int> CreateAsync(Customer customer);
        Task<bool> UpdateAsync(Customer customer);
        Task<bool> DeleteAsync(int id); // soft delete
        Task<bool> UpdateOutstandingBalanceAsync(int customerId, decimal newBalance);
    }
}
