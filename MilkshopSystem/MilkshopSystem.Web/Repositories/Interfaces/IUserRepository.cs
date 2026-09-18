using MilkshopSystem.Web.Models.Entities;
using MilkshopSystem.Web.Models.ViewModels;

namespace MilkshopSystem.Web.Repositories.Interfaces
{
    public interface IUserRepository
    {
        Task<User?> GetByUsernameAsync(string username);
        Task<User?> GetByIdAsync(int id);
        Task<PagedResult<User>> GetPagedAsync(string? search, int pageNumber, int pageSize);
        Task<bool> UsernameExistsAsync(string username, int? excludeUserId = null);
        Task<int> CreateAsync(User user);
        Task<bool> UpdateAsync(User user);
        Task<bool> UpdatePasswordAsync(int userId, string passwordHash);

        Task<List<UserPermission>> GetPermissionsAsync(int userId);
        Task SavePermissionsAsync(int userId, List<UserPermission> permissions);
    }
}
