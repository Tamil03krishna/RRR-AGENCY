using Dapper;
using MilkshopSystem.Web.Models.Entities;
using MilkshopSystem.Web.Models.ViewModels;
using MilkshopSystem.Web.Repositories.Interfaces;

namespace MilkshopSystem.Web.Repositories.Implementations
{
    public class UnitRepository : IUnitRepository
    {
        private readonly IDbConnectionFactory _factory;
        public UnitRepository(IDbConnectionFactory factory) => _factory = factory;

        public async Task<PagedResult<Unit>> GetPagedAsync(string? search, int pageNumber, int pageSize)
        {
            using var conn = _factory.CreateConnection();
            var where = string.IsNullOrWhiteSpace(search)
                ? ""
                : "WHERE Name LIKE @search OR Symbol LIKE @search";
            var searchParam = $"%{search}%";

            var total = await conn.ExecuteScalarAsync<int>(
                $"SELECT COUNT(*) FROM Units {where}", new { search = searchParam });

            var items = await conn.QueryAsync<Unit>(
                $@"SELECT * FROM Units {where}
                   ORDER BY Name
                   LIMIT @pageSize OFFSET @offset",
                new { search = searchParam, pageSize, offset = (pageNumber - 1) * pageSize });

            return new PagedResult<Unit>
            {
                Items = items.ToList(),
                TotalRecords = total,
                PageNumber = pageNumber,
                PageSize = pageSize,
                SearchTerm = search
            };
        }

        public async Task<List<Unit>> GetAllActiveAsync()
        {
            using var conn = _factory.CreateConnection();
            var result = await conn.QueryAsync<Unit>(
                "SELECT * FROM Units WHERE IsActive = 1 ORDER BY Name");
            return result.ToList();
        }

        public async Task<Unit?> GetByIdAsync(int id)
        {
            using var conn = _factory.CreateConnection();
            return await conn.QueryFirstOrDefaultAsync<Unit>("SELECT * FROM Units WHERE Id = @id", new { id });
        }

        public async Task<int> CreateAsync(Unit unit)
        {
            using var conn = _factory.CreateConnection();
            var sql = @"INSERT INTO Units (Name, Symbol, IsActive, CreatedDate)
                        VALUES (@Name, @Symbol, 1, NOW());
                        SELECT LAST_INSERT_ID();";
            return await conn.ExecuteScalarAsync<int>(sql, unit);
        }

        public async Task<bool> UpdateAsync(Unit unit)
        {
            using var conn = _factory.CreateConnection();
            var rows = await conn.ExecuteAsync(
                @"UPDATE Units SET Name=@Name, Symbol=@Symbol, IsActive=@IsActive, UpdatedDate=NOW()
                  WHERE Id=@Id", unit);
            return rows > 0;
        }

        public async Task<bool> DeleteAsync(int id)
        {
            using var conn = _factory.CreateConnection();
            var rows = await conn.ExecuteAsync("UPDATE Units SET IsActive = 0 WHERE Id = @id", new { id });
            return rows > 0;
        }
    }
}
