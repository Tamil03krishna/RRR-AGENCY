using MilkshopSystem.Web.Models.Entities;
using MilkshopSystem.Web.Models.ViewModels;
namespace MilkshopSystem.Web.Repositories.Interfaces
{
    public interface ICustomerRepository
    {
        Task<PagedResult<Customer>> GetPagedAsync(string? search, int pageNumber, int pageSize);
        Task<Customer?> GetByIdAsync(int id);
        Task<List<Customer>> SearchAsync(string term, int maxResults = 10); 
        Task<int> CreateAsync(Customer customer);
        Task<bool> UpdateAsync(Customer customer);
        Task<bool> DeleteAsync(int id); 
        Task<bool> UpdateOutstandingBalanceAsync(int customerId, decimal newBalance);
    }
}
