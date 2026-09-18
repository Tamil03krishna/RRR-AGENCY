using MilkshopSystem.Web.Models.Entities;
using MilkshopSystem.Web.Models.ViewModels;

namespace MilkshopSystem.Web.Repositories.Interfaces
{
    public interface IRoleRepository
    {
        Task<PagedResult<Role>> GetPagedAsync(string? search, int pageNumber, int pageSize);
        Task<List<Role>> GetAllActiveAsync();
        Task<Role?> GetByIdAsync(int id);
        Task<int> CreateAsync(Role role);
        Task<bool> UpdateAsync(Role role);
        Task<bool> DeleteAsync(int id);
    }
}
