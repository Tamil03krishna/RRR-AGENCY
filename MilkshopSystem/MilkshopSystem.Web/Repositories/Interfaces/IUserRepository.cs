using MilkshopSystem.Web.Models.Entities;
namespace MilkshopSystem.Web.Repositories.Interfaces
{
    public interface IUserRepository
    {
        Task<User?> GetByUsernameAsync(string username);
        Task<User?> GetByIdAsync(int id);
    }
}
