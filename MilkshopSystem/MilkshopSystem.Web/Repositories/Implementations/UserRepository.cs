using Dapper;
using MilkshopSystem.Web.Models.Entities;
using MilkshopSystem.Web.Repositories.Interfaces;

namespace MilkshopSystem.Web.Repositories.Implementations
{
    public class UserRepository : IUserRepository
    {
        private readonly IDbConnectionFactory _factory;
        public UserRepository(IDbConnectionFactory factory) => _factory = factory;

        public async Task<User?> GetByUsernameAsync(string username)
        {
            using var conn = _factory.CreateConnection();
            return await conn.QueryFirstOrDefaultAsync<User>(
                "SELECT * FROM Users WHERE Username = @username AND IsActive = 1",
                new { username });
        }

        public async Task<User?> GetByIdAsync(int id)
        {
            using var conn = _factory.CreateConnection();
            return await conn.QueryFirstOrDefaultAsync<User>(
                "SELECT * FROM Users WHERE Id = @id", new { id });
        }
    }
}
