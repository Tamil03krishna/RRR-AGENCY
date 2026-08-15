using MilkshopSystem.Web.Models.Entities;
using MilkshopSystem.Web.Models.ViewModels;
namespace MilkshopSystem.Web.Repositories.Interfaces
{
    public interface IUnitRepository
    {
        Task<PagedResult<Unit>> GetPagedAsync(string? search, int pageNumber, int pageSize);
        Task<List<Unit>> GetAllActiveAsync(); // for dropdowns
        Task<Unit?> GetByIdAsync(int id);
        Task<int> CreateAsync(Unit unit);
        Task<bool> UpdateAsync(Unit unit);
        Task<bool> DeleteAsync(int id);
    }
}
