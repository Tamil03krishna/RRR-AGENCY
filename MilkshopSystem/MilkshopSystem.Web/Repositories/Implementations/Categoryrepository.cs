using Dapper;
using MilkshopSystem.Web.Models.Entities;
using MilkshopSystem.Web.Models.ViewModels;
using MilkshopSystem.Web.Repositories.Interfaces;

namespace MilkshopSystem.Web.Repositories.Implementations
{
    public class CategoryRepository : ICategoryRepository
    {
        private readonly IDbConnectionFactory _factory;
        public CategoryRepository(IDbConnectionFactory factory) => _factory = factory;

        public async Task<PagedResult<ProductCategory>> GetPagedAsync(string? search, int pageNumber, int pageSize)
        {
            using var conn = _factory.CreateConnection();
            var where = string.IsNullOrWhiteSpace(search)
                ? ""
                : "WHERE Name LIKE @search";
            var searchParam = $"%{search}%";

            var total = await conn.ExecuteScalarAsync<int>(
                $"SELECT COUNT(*) FROM ProductCategories {where}", new { search = searchParam });

            var items = await conn.QueryAsync<ProductCategory>(
                $@"SELECT * FROM ProductCategories {where}
                   ORDER BY Name
                   LIMIT @pageSize OFFSET @offset",
                new { search = searchParam, pageSize, offset = (pageNumber - 1) * pageSize });

            return new PagedResult<ProductCategory>
            {
                Items = items.ToList(),
                TotalRecords = total,
                PageNumber = pageNumber,
                PageSize = pageSize,
                SearchTerm = search
            };
        }

        public async Task<List<ProductCategory>> GetAllActiveAsync()
        {
            using var conn = _factory.CreateConnection();
            var result = await conn.QueryAsync<ProductCategory>(
                "SELECT * FROM ProductCategories WHERE IsActive = 1 ORDER BY Name");
            return result.ToList();
        }

        public async Task<ProductCategory?> GetByIdAsync(int id)
        {
            using var conn = _factory.CreateConnection();
            return await conn.QueryFirstOrDefaultAsync<ProductCategory>(
                "SELECT * FROM ProductCategories WHERE Id = @id", new { id });
        }

        public async Task<int> CreateAsync(ProductCategory category)
        {
            using var conn = _factory.CreateConnection();
            var sql = @"INSERT INTO ProductCategories (Name, IsActive, CreatedDate)
                        VALUES (@Name, 1, NOW());
                        SELECT LAST_INSERT_ID();";
            return await conn.ExecuteScalarAsync<int>(sql, category);
        }

        public async Task<bool> UpdateAsync(ProductCategory category)
        {
            using var conn = _factory.CreateConnection();
            var rows = await conn.ExecuteAsync(
                @"UPDATE ProductCategories SET Name=@Name, IsActive=@IsActive, UpdatedDate=NOW()
                  WHERE Id=@Id", category);
            return rows > 0;
        }

        public async Task<bool> DeleteAsync(int id)
        {
            using var conn = _factory.CreateConnection();
            var rows = await conn.ExecuteAsync(
                "UPDATE ProductCategories SET IsActive = 0 WHERE Id = @id", new { id });
            return rows > 0;
        }
    }
}