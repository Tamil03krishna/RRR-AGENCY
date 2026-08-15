using Dapper;
using MilkshopSystem.Web.Repositories.Interfaces;

namespace MilkshopSystem.Web.Repositories.Implementations
{
    public class DashboardRepository : IDashboardRepository
    {
        private readonly IDbConnectionFactory _factory;
        public DashboardRepository(IDbConnectionFactory factory) => _factory = factory;

        public async Task<DashboardSummary> GetSummaryAsync()
        {
            using var conn = _factory.CreateConnection();

            var summary = new DashboardSummary
            {
                TodaySales = await conn.ExecuteScalarAsync<decimal>(
                    "SELECT COALESCE(SUM(SubTotal),0) FROM Invoices WHERE DATE(InvoiceDate)=CURDATE() AND IsCancelled=0"),

                MonthSales = await conn.ExecuteScalarAsync<decimal>(
                    @"SELECT COALESCE(SUM(SubTotal),0) FROM Invoices
                      WHERE MONTH(InvoiceDate)=MONTH(CURDATE()) AND YEAR(InvoiceDate)=YEAR(CURDATE()) AND IsCancelled=0"),

                // point 12: stock value = sum(current stock * store price) across all products
                TotalStockValue = await conn.ExecuteScalarAsync<decimal>(
                    "SELECT COALESCE(SUM(s.CurrentStock * p.StorePrice),0) FROM Stocks s JOIN Products p ON p.Id = s.ProductId"),

                LowStockCount = await conn.ExecuteScalarAsync<int>(
                    "SELECT COUNT(*) FROM Stocks WHERE CurrentStock <= LowStockLevel"),

                TotalCustomers = await conn.ExecuteScalarAsync<int>(
                    "SELECT COUNT(*) FROM Customers WHERE IsActive = 1"),

                TotalOutstanding = await conn.ExecuteScalarAsync<decimal>(
                    "SELECT COALESCE(SUM(OutstandingBalance),0) FROM Customers WHERE IsActive = 1"),

                TotalProducts = await conn.ExecuteScalarAsync<int>(
                    "SELECT COUNT(*) FROM Products WHERE IsActive = 1")
            };

            return summary;
        }
    }
}
