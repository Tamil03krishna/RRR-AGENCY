using Dapper;
using MilkshopSystem.Web.Models.Entities;
using MilkshopSystem.Web.Models.ViewModels;
using MilkshopSystem.Web.Repositories.Interfaces;

namespace MilkshopSystem.Web.Repositories.Implementations
{
    public class RoleRepository : IRoleRepository
    {
        private readonly IDbConnectionFactory _factory;
        public RoleRepository(IDbConnectionFactory factory) => _factory = factory;

        public async Task<PagedResult<Role>> GetPagedAsync(string? search, int pageNumber, int pageSize)
        {
            using var conn = _factory.CreateConnection();
            var where = string.IsNullOrWhiteSpace(search) ? "" : "WHERE Name LIKE @search";
            var searchParam = $"%{search}%";

            var total = await conn.ExecuteScalarAsync<int>(
                $"SELECT COUNT(*) FROM Roles {where}", new { search = searchParam });

            var items = await conn.QueryAsync<Role>(
                $@"SELECT * FROM Roles {where} ORDER BY Name
                   LIMIT @pageSize OFFSET @offset",
                new { search = searchParam, pageSize, offset = (pageNumber - 1) * pageSize });

            return new PagedResult<Role>
            {
                Items = items.ToList(), TotalRecords = total, PageNumber = pageNumber, PageSize = pageSize, SearchTerm = search
            };
        }

        public async Task<List<Role>> GetAllActiveAsync()
        {
            using var conn = _factory.CreateConnection();
            var result = await conn.QueryAsync<Role>("SELECT * FROM Roles WHERE IsActive = 1 ORDER BY Name");
            return result.ToList();
        }

        public async Task<Role?> GetByIdAsync(int id)
        {
            using var conn = _factory.CreateConnection();
            return await conn.QueryFirstOrDefaultAsync<Role>("SELECT * FROM Roles WHERE Id = @id", new { id });
        }

        public async Task<int> CreateAsync(Role role)
        {
            using var conn = _factory.CreateConnection();
            return await conn.ExecuteScalarAsync<int>(
                @"INSERT INTO Roles (Name, IsActive, CreatedDate) VALUES (@Name, 1, NOW());
                  SELECT LAST_INSERT_ID();", role);
        }

        public async Task<bool> UpdateAsync(Role role)
        {
            using var conn = _factory.CreateConnection();
            var rows = await conn.ExecuteAsync(
                "UPDATE Roles SET Name=@Name, IsActive=@IsActive, UpdatedDate=NOW() WHERE Id=@Id", role);
            return rows > 0;
        }

        public async Task<bool> DeleteAsync(int id)
        {
            using var conn = _factory.CreateConnection();
            var rows = await conn.ExecuteAsync("UPDATE Roles SET IsActive = 0 WHERE Id = @id", new { id });
            return rows > 0;
        }
    }
}
