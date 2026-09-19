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

                TotalStockValue = await conn.ExecuteScalarAsync<decimal>(
                    "SELECT COALESCE(SUM(s.CurrentStock * p.StorePrice),0) FROM Stocks s JOIN Products p ON p.Id = s.ProductId"),
                CurrentStock= await conn.ExecuteScalarAsync<int>(
                    "SELECT COALESCE(SUM(s.CurrentStock),0) FROM Stocks s JOIN Products p ON p.Id = s.ProductId"),
                LowStockCount = await conn.ExecuteScalarAsync<int>(
                    "SELECT COUNT(*) FROM Stocks WHERE CurrentStock <= LowStockLevel"),

                TotalCustomers = await conn.ExecuteScalarAsync<int>(
                    "SELECT COUNT(*) FROM Customers WHERE IsActive = 1"),

                TotalOutstanding = await conn.ExecuteScalarAsync<decimal>(
                    "SELECT COALESCE(SUM(OutstandingBalance),0) FROM Customers WHERE IsActive = 1"),

                TotalProducts = await conn.ExecuteScalarAsync<int>(
                    "SELECT COUNT(*) FROM Products WHERE IsActive = 1")
            };

            var recentInvoices = await conn.QueryAsync<RecentInvoiceItem>(
                @"SELECT i.Id, i.InvoiceNo, c.Name AS CustomerName, i.GrandTotal, i.PaymentStatus, i.InvoiceDate
                  FROM Invoices i
                  JOIN Customers c ON c.Id = i.CustomerId
                  WHERE i.IsCancelled = 0
                  ORDER BY i.InvoiceDate DESC
                  LIMIT 5");
            summary.RecentInvoices = recentInvoices.ToList();

            var recentPayments = await conn.QueryAsync<RecentPaymentItem>(
                @"SELECT c.Name AS CustomerName, ip.Amount, pm.Name AS PaymentModeName, ip.PaymentDate
                  FROM InvoicePayments ip
                  JOIN Customers c ON c.Id = ip.CustomerId
                  LEFT JOIN PaymentModes pm ON pm.Id = ip.PaymentModeId
                  ORDER BY ip.PaymentDate DESC
                  LIMIT 5");
            summary.RecentPayments = recentPayments.ToList();

            return summary;
        }
    }
}