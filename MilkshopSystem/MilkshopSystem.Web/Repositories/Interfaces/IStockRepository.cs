using MilkshopSystem.Web.Models.Entities;
using MilkshopSystem.Web.Models.ViewModels;
using System.Data;
namespace MilkshopSystem.Web.Repositories.Interfaces
{
    public interface IStockRepository
    {
        Task<PagedResult<Stock>> GetPagedAsync(string? search, int pageNumber, int pageSize);
        Task<Stock?> GetByProductIdAsync(int productId);
        Task CreateForProductAsync(int productId, decimal openingStock, decimal lowStockLevel);
        Task<bool> UpdateAsync(Stock stock);
        Task<List<Stock>> GetLowStockAsync();
        Task<decimal> GetTotalStockValueAsync(); 
        Task ReduceStockAsync(IDbConnection conn, IDbTransaction tx, int productId, decimal qty);
        Task<List<ProductStockViewModel>> GetProductStockListAsync();

    }
}
