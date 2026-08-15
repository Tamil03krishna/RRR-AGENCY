using MilkshopSystem.Web.Models.Entities;
using MilkshopSystem.Web.Models.ViewModels;
namespace MilkshopSystem.Web.Repositories.Interfaces
{
    public interface IProductRepository
    {
        Task<PagedResult<Product>> GetPagedAsync(string? search, int pageNumber, int pageSize);
        Task<Product?> GetByIdAsync(int id);
        Task<List<Product>> SearchActiveAsync(string term, int maxResults = 10); // for billing dropdown
        Task<List<Models.Entities.ProductCategory>> GetCategoriesAsync();
        Task<int> CreateAsync(Product product);
        Task<bool> UpdateAsync(Product product);
        Task<bool> DeleteAsync(int id);
    }
}
