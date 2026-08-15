using Dapper;
using MilkshopSystem.Web.Models.Entities;
using MilkshopSystem.Web.Models.ViewModels;
using MilkshopSystem.Web.Repositories.Interfaces;
using System.Data;

namespace MilkshopSystem.Web.Repositories.Implementations
{
    public class StockRepository : IStockRepository
    {
        private readonly IDbConnectionFactory _factory;
        public StockRepository(IDbConnectionFactory factory) => _factory = factory;

        private const string BaseSelect = @"
            SELECT s.*, p.Name AS ProductName, p.Size, u.Symbol AS UnitSymbol
            FROM Stocks s
            JOIN Products p ON p.Id = s.ProductId
            JOIN Units u ON u.Id = p.UnitId";

        public async Task<PagedResult<Stock>> GetPagedAsync(string? search, int pageNumber, int pageSize)
        {
            using var conn = _factory.CreateConnection();
            var where = string.IsNullOrWhiteSpace(search) ? "" : "WHERE p.Name LIKE @search";
            var searchParam = $"%{search}%";

            var total = await conn.ExecuteScalarAsync<int>(
                $"SELECT COUNT(*) FROM Stocks s JOIN Products p ON p.Id = s.ProductId {where}",
                new { search = searchParam });

            var items = await conn.QueryAsync<Stock>(
                $@"{BaseSelect} {where} ORDER BY p.Name LIMIT @pageSize OFFSET @offset",
                new { search = searchParam, pageSize, offset = (pageNumber - 1) * pageSize });

            return new PagedResult<Stock>
            {
                Items = items.ToList(), TotalRecords = total, PageNumber = pageNumber, PageSize = pageSize, SearchTerm = search
            };
        }

        public async Task<Stock?> GetByProductIdAsync(int productId)
        {
            using var conn = _factory.CreateConnection();
            return await conn.QueryFirstOrDefaultAsync<Stock>($"{BaseSelect} WHERE s.ProductId = @productId", new { productId });
        }

        // called right after a product is created, so every product always has a stock row
        public async Task CreateForProductAsync(int productId, decimal openingStock, decimal lowStockLevel)
        {
            using var conn = _factory.CreateConnection();
            await conn.ExecuteAsync(
                @"INSERT INTO Stocks (ProductId, OpeningStock, CurrentStock, LowStockLevel, LastUpdated)
                  VALUES (@productId, @openingStock, @openingStock, @lowStockLevel, NOW())",
                new { productId, openingStock, lowStockLevel });
        }

        public async Task<bool> UpdateAsync(Stock stock)
        {
            using var conn = _factory.CreateConnection();
            var rows = await conn.ExecuteAsync(
                @"UPDATE Stocks SET CurrentStock=@CurrentStock, LowStockLevel=@LowStockLevel, LastUpdated=NOW()
                  WHERE Id=@Id", stock);
            return rows > 0;
        }

        public async Task<List<Stock>> GetLowStockAsync()
        {
            using var conn = _factory.CreateConnection();
            var result = await conn.QueryAsync<Stock>($"{BaseSelect} WHERE s.CurrentStock <= s.LowStockLevel ORDER BY p.Name");
            return result.ToList();
        }

        public async Task<decimal> GetTotalStockValueAsync()
        {
            using var conn = _factory.CreateConnection();
            return await conn.ExecuteScalarAsync<decimal>(
                @"SELECT COALESCE(SUM(s.CurrentStock * p.StorePrice), 0)
                  FROM Stocks s JOIN Products p ON p.Id = s.ProductId");
        }

        // Called inside the Billing transaction. Locks the row (FOR UPDATE) so two
        // simultaneous bills can't both oversell the same stock.
        public async Task ReduceStockAsync(IDbConnection conn, IDbTransaction tx, int productId, decimal qty)
        {
            var currentStock = await conn.ExecuteScalarAsync<decimal?>(
                "SELECT CurrentStock FROM Stocks WHERE ProductId = @productId FOR UPDATE",
                new { productId }, tx);

            if (currentStock is null)
                throw new InvalidOperationException("Stock record not found for this product.");

            if (currentStock.Value < qty)
                throw new InvalidOperationException($"Not enough stock. Available: {currentStock.Value}, requested: {qty}");

            await conn.ExecuteAsync(
                "UPDATE Stocks SET CurrentStock = CurrentStock - @qty, LastUpdated = NOW() WHERE ProductId = @productId",
                new { qty, productId }, tx);
        }
    }
}
