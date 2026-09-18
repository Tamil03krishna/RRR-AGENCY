using MilkshopSystem.Web.Models.Entities;
using MilkshopSystem.Web.Models.ViewModels;

namespace MilkshopSystem.Web.Repositories.Interfaces
{
    public interface ICategoryRepository
    {
        Task<PagedResult<ProductCategory>> GetPagedAsync(string? search, int pageNumber, int pageSize);
        Task<List<ProductCategory>> GetAllActiveAsync();
        Task<ProductCategory?> GetByIdAsync(int id);
        Task<int> CreateAsync(ProductCategory category);
        Task<bool> UpdateAsync(ProductCategory category);
        Task<bool> DeleteAsync(int id);
    }
}