using Dapper;
using MilkshopSystem.Web.Models.Entities;
using MilkshopSystem.Web.Repositories.Interfaces;

namespace MilkshopSystem.Web.Repositories.Implementations
{
    public class NoteRepository : INoteRepository
    {
        private readonly IDbConnectionFactory _factory;
        public NoteRepository(IDbConnectionFactory factory) => _factory = factory;

        public async Task<List<Note>> GetByUserAsync(int userId)
        {
            using var conn = _factory.CreateConnection();
            var notes = await conn.QueryAsync<Note>(
                @"SELECT * FROM Notes WHERE UserId = @userId
                  ORDER BY IsPinned DESC, CreatedDate DESC",
                new { userId });
            return notes.ToList();
        }

        public async Task<Note?> GetByIdAsync(int id, int userId)
        {
            using var conn = _factory.CreateConnection();
            return await conn.QueryFirstOrDefaultAsync<Note>(
                "SELECT * FROM Notes WHERE Id = @id AND UserId = @userId",
                new { id, userId });
        }

        public async Task<int> CreateAsync(Note note)
        {
            using var conn = _factory.CreateConnection();
            return await conn.ExecuteScalarAsync<int>(
                @"INSERT INTO Notes (UserId, Content, Color, IsPinned, CreatedDate)
                  VALUES (@UserId, @Content, @Color, @IsPinned, NOW());
                  SELECT LAST_INSERT_ID();", note);
        }

        public async Task<bool> UpdateAsync(Note note)
        {
            using var conn = _factory.CreateConnection();
            var rows = await conn.ExecuteAsync(
                @"UPDATE Notes SET Content = @Content, Color = @Color, IsPinned = @IsPinned, UpdatedDate = NOW()
                  WHERE Id = @Id AND UserId = @UserId", note);
            return rows > 0;
        }

        public async Task<bool> DeleteAsync(int id, int userId)
        {
            using var conn = _factory.CreateConnection();
            var rows = await conn.ExecuteAsync(
                "DELETE FROM Notes WHERE Id = @id AND UserId = @userId", new { id, userId });
            return rows > 0;
        }
    }
}