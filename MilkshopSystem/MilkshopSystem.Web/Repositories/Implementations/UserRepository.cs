using Dapper;
using MilkshopSystem.Web.Models.Entities;
using MilkshopSystem.Web.Models.ViewModels;
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
                @"SELECT u.*, r.Name AS RoleName FROM Users u
                  LEFT JOIN Roles r ON r.Id = u.RoleId
                  WHERE u.Id = @id", new { id });
        }

        public async Task<PagedResult<User>> GetPagedAsync(string? search, int pageNumber, int pageSize)
        {
            using var conn = _factory.CreateConnection();
            var where = string.IsNullOrWhiteSpace(search)
                ? ""
                : "WHERE u.Username LIKE @search OR u.FullName LIKE @search";
            var searchParam = $"%{search}%";

            var total = await conn.ExecuteScalarAsync<int>(
                $"SELECT COUNT(*) FROM Users u {where}", new { search = searchParam });

            var items = await conn.QueryAsync<User>(
                $@"SELECT u.*, r.Name AS RoleName FROM Users u
                   LEFT JOIN Roles r ON r.Id = u.RoleId
                   {where}
                   ORDER BY u.Username
                   LIMIT @pageSize OFFSET @offset",
                new { search = searchParam, pageSize, offset = (pageNumber - 1) * pageSize });

            return new PagedResult<User>
            {
                Items = items.ToList(), TotalRecords = total, PageNumber = pageNumber, PageSize = pageSize, SearchTerm = search
            };
        }

        public async Task<bool> UsernameExistsAsync(string username, int? excludeUserId = null)
        {
            using var conn = _factory.CreateConnection();
            var count = await conn.ExecuteScalarAsync<int>(
                "SELECT COUNT(*) FROM Users WHERE Username = @username AND (@excludeUserId IS NULL OR Id != @excludeUserId)",
                new { username, excludeUserId });
            return count > 0;
        }

        public async Task<int> CreateAsync(User user)
        {
            using var conn = _factory.CreateConnection();
            return await conn.ExecuteScalarAsync<int>(
                @"INSERT INTO Users (Username, PasswordHash, FullName, Role, RoleId, IsActive, CreatedDate)
                  VALUES (@Username, @PasswordHash, @FullName, @Role, @RoleId, @IsActive, NOW());
                  SELECT LAST_INSERT_ID();", user);
        }

        public async Task<bool> UpdateAsync(User user)
        {
            using var conn = _factory.CreateConnection();
            var rows = await conn.ExecuteAsync(
                @"UPDATE Users SET FullName=@FullName, Role=@Role, RoleId=@RoleId, IsActive=@IsActive, UpdatedDate=NOW()
                  WHERE Id=@Id", user);
            return rows > 0;
        }

        public async Task<bool> UpdatePasswordAsync(int userId, string passwordHash)
        {
            using var conn = _factory.CreateConnection();
            var rows = await conn.ExecuteAsync(
                "UPDATE Users SET PasswordHash = @passwordHash, UpdatedDate = NOW() WHERE Id = @userId",
                new { passwordHash, userId });
            return rows > 0;
        }

        public async Task<List<UserPermission>> GetPermissionsAsync(int userId)
        {
            using var conn = _factory.CreateConnection();
            var result = await conn.QueryAsync<UserPermission>(
                "SELECT * FROM UserPermissions WHERE UserId = @userId", new { userId });
            return result.ToList();
        }

        public async Task SavePermissionsAsync(int userId, List<UserPermission> permissions)
        {
            using var conn = (MySqlConnector.MySqlConnection)_factory.CreateConnection();
            using var tx = await conn.BeginTransactionAsync();
            try
            {
                await conn.ExecuteAsync("DELETE FROM UserPermissions WHERE UserId = @userId", new { userId }, tx);

                foreach (var p in permissions)
                {
                    await conn.ExecuteAsync(
                        @"INSERT INTO UserPermissions (UserId, ModuleName, CanView, CanAdd, CanEdit, CanDelete)
                          VALUES (@userId, @ModuleName, @CanView, @CanAdd, @CanEdit, @CanDelete)",
                        new { userId, p.ModuleName, p.CanView, p.CanAdd, p.CanEdit, p.CanDelete }, tx);
                }

                await tx.CommitAsync();
            }
            catch
            {
                await tx.RollbackAsync();
                throw;
            }
        }
    }
}
