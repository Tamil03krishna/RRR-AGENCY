using MilkshopSystem.Web.Models.Entities;

namespace MilkshopSystem.Web.Repositories.Interfaces
{
    public interface INoteRepository
    {
        Task<List<Note>> GetByUserAsync(int userId);
        Task<Note?> GetByIdAsync(int id, int userId);
        Task<int> CreateAsync(Note note);
        Task<bool> UpdateAsync(Note note);
        Task<bool> DeleteAsync(int id, int userId);
    }
}