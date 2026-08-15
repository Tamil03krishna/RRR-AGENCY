using Dapper;
using MilkshopSystem.Web.Models.Entities;
using MilkshopSystem.Web.Models.ViewModels;
using MilkshopSystem.Web.Repositories.Interfaces;

namespace MilkshopSystem.Web.Repositories.Implementations
{
    public class PaymentModeRepository : IPaymentModeRepository
    {
        private readonly IDbConnectionFactory _factory;
        public PaymentModeRepository(IDbConnectionFactory factory) => _factory = factory;

        public async Task<PagedResult<PaymentMode>> GetPagedAsync(string? search, int pageNumber, int pageSize)
        {
            using var conn = _factory.CreateConnection();
            var where = string.IsNullOrWhiteSpace(search) ? "" : "WHERE Name LIKE @search";
            var searchParam = $"%{search}%";

            var total = await conn.ExecuteScalarAsync<int>($"SELECT COUNT(*) FROM PaymentModes {where}", new { search = searchParam });
            var items = await conn.QueryAsync<PaymentMode>(
                $"SELECT * FROM PaymentModes {where} ORDER BY Name LIMIT @pageSize OFFSET @offset",
                new { search = searchParam, pageSize, offset = (pageNumber - 1) * pageSize });

            return new PagedResult<PaymentMode>
            {
                Items = items.ToList(), TotalRecords = total, PageNumber = pageNumber, PageSize = pageSize, SearchTerm = search
            };
        }

        public async Task<List<PaymentMode>> GetAllActiveAsync()
        {
            using var conn = _factory.CreateConnection();
            var result = await conn.QueryAsync<PaymentMode>("SELECT * FROM PaymentModes WHERE IsActive = 1 ORDER BY Name");
            return result.ToList();
        }

        public async Task<PaymentMode?> GetByIdAsync(int id)
        {
            using var conn = _factory.CreateConnection();
            return await conn.QueryFirstOrDefaultAsync<PaymentMode>("SELECT * FROM PaymentModes WHERE Id = @id", new { id });
        }

        public async Task<int> CreateAsync(PaymentMode mode)
        {
            using var conn = _factory.CreateConnection();
            var sql = "INSERT INTO PaymentModes (Name, IsActive, CreatedDate) VALUES (@Name, 1, NOW()); SELECT LAST_INSERT_ID();";
            return await conn.ExecuteScalarAsync<int>(sql, mode);
        }

        public async Task<bool> UpdateAsync(PaymentMode mode)
        {
            using var conn = _factory.CreateConnection();
            var rows = await conn.ExecuteAsync("UPDATE PaymentModes SET Name=@Name, IsActive=@IsActive WHERE Id=@Id", mode);
            return rows > 0;
        }

        public async Task<bool> DeleteAsync(int id)
        {
            using var conn = _factory.CreateConnection();
            var rows = await conn.ExecuteAsync("UPDATE PaymentModes SET IsActive = 0 WHERE Id = @id", new { id });
            return rows > 0;
        }
    }
}
