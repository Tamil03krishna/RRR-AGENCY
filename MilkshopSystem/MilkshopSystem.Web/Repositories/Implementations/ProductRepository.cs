using Dapper;
using MilkshopSystem.Web.Models.Entities;
using MilkshopSystem.Web.Models.ViewModels;
using MilkshopSystem.Web.Repositories.Interfaces;

namespace MilkshopSystem.Web.Repositories.Implementations
{
    public class ProductRepository : IProductRepository
    {
        private readonly IDbConnectionFactory _factory;
        public ProductRepository(IDbConnectionFactory factory) => _factory = factory;

        private const string BaseSelect = @"
            SELECT p.*, c.Name AS CategoryName, u.Name AS UnitName, u.Symbol AS UnitSymbol
            FROM Products p
            JOIN ProductCategories c ON c.Id = p.CategoryId
            JOIN Units u ON u.Id = p.UnitId";

        public async Task<PagedResult<Product>> GetPagedAsync(string? search, int pageNumber, int pageSize)
        {
            using var conn = _factory.CreateConnection();
            var where = string.IsNullOrWhiteSpace(search)
                ? "WHERE p.IsActive = 1"
                : "WHERE p.IsActive = 1 AND p.Name LIKE @search";
            var searchParam = $"%{search}%";

            var total = await conn.ExecuteScalarAsync<int>(
                $"SELECT COUNT(*) FROM Products p {where}", new { search = searchParam });

            var items = await conn.QueryAsync<Product>(
                $@"{BaseSelect} {where} ORDER BY p.Name LIMIT @pageSize OFFSET @offset",
                new { search = searchParam, pageSize, offset = (pageNumber - 1) * pageSize });

            return new PagedResult<Product>
            {
                Items = items.ToList(), TotalRecords = total, PageNumber = pageNumber, PageSize = pageSize, SearchTerm = search
            };
        }

        public async Task<Product?> GetByIdAsync(int id)
        {
            using var conn = _factory.CreateConnection();
            return await conn.QueryFirstOrDefaultAsync<Product>($"{BaseSelect} WHERE p.Id = @id", new { id });
        }

        public async Task<List<Product>> SearchActiveAsync(string term, int maxResults = 10)
        {
            using var conn = _factory.CreateConnection();
            var result = await conn.QueryAsync<Product>(
                $@"{BaseSelect}
                   WHERE p.IsActive = 1
                     AND p.ActiveFrom <= CURDATE()
                     AND (p.ActiveTo IS NULL OR p.ActiveTo >= CURDATE())
                     AND p.Name LIKE @term
                   ORDER BY p.Name LIMIT @maxResults",
                new { term = $"%{term}%", maxResults });
            return result.ToList();
        }

        public async Task<List<ProductCategory>> GetCategoriesAsync()
        {
            using var conn = _factory.CreateConnection();
            var result = await conn.QueryAsync<ProductCategory>("SELECT * FROM ProductCategories WHERE IsActive = 1 ORDER BY Name");
            return result.ToList();
        }

        public async Task<int> CreateAsync(Product product)
        {
            using var conn = _factory.CreateConnection();
            var sql = @"INSERT INTO Products
                        (Name, CategoryId, UnitId, Size, StorePrice, MrpPrice, ActiveFrom, ActiveTo, IsActive, CreatedDate)
                        VALUES
                        (@Name, @CategoryId, @UnitId, @Size, @StorePrice, @MrpPrice, @ActiveFrom, @ActiveTo, 1, NOW());
                        SELECT LAST_INSERT_ID();";
            return await conn.ExecuteScalarAsync<int>(sql, product);
        }

        public async Task<bool> UpdateAsync(Product product)
        {
            using var conn = _factory.CreateConnection();
            var rows = await conn.ExecuteAsync(
                @"UPDATE Products SET
                    Name=@Name, CategoryId=@CategoryId, UnitId=@UnitId, Size=@Size,
                    StorePrice=@StorePrice, MrpPrice=@MrpPrice,
                    ActiveFrom=@ActiveFrom, ActiveTo=@ActiveTo, IsActive=@IsActive, UpdatedDate=NOW()
                  WHERE Id=@Id", product);
            return rows > 0;
        }

        public async Task<bool> DeleteAsync(int id)
        {
            using var conn = _factory.CreateConnection();
            var rows = await conn.ExecuteAsync("UPDATE Products SET IsActive = 0 WHERE Id = @id", new { id });
            return rows > 0;
        }
    }
}
