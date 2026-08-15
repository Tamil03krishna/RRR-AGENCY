using Dapper;
using MilkshopSystem.Web.Models.Entities;
using MilkshopSystem.Web.Models.ViewModels;
using MilkshopSystem.Web.Repositories.Interfaces;
using MySqlConnector;

namespace MilkshopSystem.Web.Repositories.Implementations
{
    public class InvoiceRepository : IInvoiceRepository
    {
        private readonly IDbConnectionFactory _factory;
        private readonly IStockRepository _stockRepository;
        private readonly ICustomerRepository _customerRepository;

        public InvoiceRepository(IDbConnectionFactory factory, IStockRepository stockRepository, ICustomerRepository customerRepository)
        {
            _factory = factory;
            _stockRepository = stockRepository;
            _customerRepository = customerRepository;
        }

        public async Task<PagedResult<Invoice>> GetPagedAsync(string? search, int pageNumber, int pageSize)
        {
            using var conn = _factory.CreateConnection();
            var where = string.IsNullOrWhiteSpace(search)
                ? ""
                : "WHERE i.InvoiceNo LIKE @search OR c.Name LIKE @search OR c.Phone LIKE @search OR i.PaymentStatus LIKE @search";
            var searchParam = $"%{search}%";

            var total = await conn.ExecuteScalarAsync<int>(
                $"SELECT COUNT(*) FROM Invoices i JOIN Customers c ON c.Id = i.CustomerId {where}",
                new { search = searchParam });

            var items = await conn.QueryAsync<Invoice>(
                $@"SELECT i.*, c.Name AS CustomerName, c.Phone AS CustomerPhone
                   FROM Invoices i JOIN Customers c ON c.Id = i.CustomerId
                   {where}
                   ORDER BY i.InvoiceDate DESC
                   LIMIT @pageSize OFFSET @offset",
                new { search = searchParam, pageSize, offset = (pageNumber - 1) * pageSize });

            return new PagedResult<Invoice>
            {
                Items = items.ToList(), TotalRecords = total, PageNumber = pageNumber, PageSize = pageSize, SearchTerm = search
            };
        }

        public async Task<Invoice?> GetByIdAsync(int id)
        {
            using var conn = _factory.CreateConnection();
            var invoice = await conn.QueryFirstOrDefaultAsync<Invoice>(
                @"SELECT i.*, c.Name AS CustomerName, c.Phone AS CustomerPhone
                  FROM Invoices i JOIN Customers c ON c.Id = i.CustomerId
                  WHERE i.Id = @id", new { id });

            if (invoice is null) return null;

            var items = await conn.QueryAsync<InvoiceItem>(
                @"SELECT ii.*, p.Name AS ProductName, p.Size, u.Symbol AS UnitSymbol
                  FROM InvoiceItems ii
                  JOIN Products p ON p.Id = ii.ProductId
                  JOIN Units u ON u.Id = p.UnitId
                  WHERE ii.InvoiceId = @id", new { id });

            invoice.Items = items.ToList();
            return invoice;
        }

        public async Task<string> GetNextInvoiceNoAsync()
        {
            using var conn = _factory.CreateConnection();
            var year = DateTime.Now.Year;
            var count = await conn.ExecuteScalarAsync<int>(
                "SELECT COUNT(*) FROM Invoices WHERE YEAR(InvoiceDate) = @year", new { year });
            return $"INV-{year}-{(count + 1):D5}";
        }

        // Full billing transaction:
        // 1) insert invoice header + items
        // 2) reduce stock for every item (row-locked, fails if not enough stock)
        // 3) update customer's running OutstandingBalance to the new BalanceAmount
        public async Task<int> CreateInvoiceAsync(Invoice invoice)
        {
            using var conn = (MySqlConnection)_factory.CreateConnection();
            using var tx = await conn.BeginTransactionAsync();
            try
            {
                var invoiceId = await conn.ExecuteScalarAsync<int>(
                    @"INSERT INTO Invoices
                        (InvoiceNo, CustomerId, InvoiceDate, SubTotal, PreviousBalance, GrandTotal,
                         PaidAmount, BalanceAmount, PaymentStatus, PaymentModeId, CreatedByUserId, CreatedDate)
                      VALUES
                        (@InvoiceNo, @CustomerId, NOW(), @SubTotal, @PreviousBalance, @GrandTotal,
                         @PaidAmount, @BalanceAmount, @PaymentStatus, @PaymentModeId, @CreatedByUserId, NOW());
                      SELECT LAST_INSERT_ID();",
                    invoice, tx);

                foreach (var item in invoice.Items)
                {
                    item.InvoiceId = invoiceId;
                    await conn.ExecuteAsync(
                        @"INSERT INTO InvoiceItems (InvoiceId, ProductId, PriceType, UnitPrice, Qty, Amount, CreatedDate)
                          VALUES (@InvoiceId, @ProductId, @PriceType, @UnitPrice, @Qty, @Amount, NOW())",
                        item, tx);

                    // point 13: stock must reduce as part of billing, blocked if insufficient
                    await _stockRepository.ReduceStockAsync(conn, tx, item.ProductId, item.Qty);
                }

                if (invoice.PaidAmount > 0)
                {
                    await conn.ExecuteAsync(
                        @"INSERT INTO InvoicePayments (InvoiceId, CustomerId, Amount, PaymentModeId, PaymentDate)
                          VALUES (@invoiceId, @customerId, @amount, @paymentModeId, NOW())",
                        new { invoiceId, customerId = invoice.CustomerId, amount = invoice.PaidAmount, paymentModeId = invoice.PaymentModeId },
                        tx);
                }

                // point 11: carry forward whatever is still owed to the customer record,
                // so it shows up as PreviousBalance next time they're billed
                await conn.ExecuteAsync(
                    "UPDATE Customers SET OutstandingBalance = @balance, UpdatedDate = NOW() WHERE Id = @customerId",
                    new { balance = invoice.BalanceAmount, customerId = invoice.CustomerId }, tx);

                await tx.CommitAsync();
                return invoiceId;
            }
            catch
            {
                await tx.RollbackAsync();
                throw;
            }
        }

        // Customer comes back later and pays off some/all of their due, without buying anything new
        public async Task<int> AddPaymentAsync(InvoicePayment payment)
        {
            using var conn = (MySqlConnection)_factory.CreateConnection();
            using var tx = await conn.BeginTransactionAsync();
            try
            {
                var paymentId = await conn.ExecuteScalarAsync<int>(
                    @"INSERT INTO InvoicePayments (InvoiceId, CustomerId, Amount, PaymentModeId, PaymentDate)
                      VALUES (@InvoiceId, @CustomerId, @Amount, @PaymentModeId, NOW());
                      SELECT LAST_INSERT_ID();", payment, tx);

                await conn.ExecuteAsync(
                    @"UPDATE Invoices
                      SET PaidAmount = PaidAmount + @Amount,
                          BalanceAmount = BalanceAmount - @Amount,
                          PaymentStatus = CASE WHEN BalanceAmount - @Amount <= 0 THEN 'Paid'
                                               WHEN PaidAmount + @Amount > 0 THEN 'Partial'
                                               ELSE 'Unpaid' END
                      WHERE Id = @InvoiceId", payment, tx);

                await conn.ExecuteAsync(
                    "UPDATE Customers SET OutstandingBalance = OutstandingBalance - @Amount, UpdatedDate = NOW() WHERE Id = @CustomerId",
                    payment, tx);

                await tx.CommitAsync();
                return paymentId;
            }
            catch
            {
                await tx.RollbackAsync();
                throw;
            }
        }

        public async Task<decimal> GetTodaySalesAsync()
        {
            using var conn = _factory.CreateConnection();
            return await conn.ExecuteScalarAsync<decimal>(
                "SELECT COALESCE(SUM(SubTotal), 0) FROM Invoices WHERE DATE(InvoiceDate) = CURDATE() AND IsCancelled = 0");
        }

        public async Task<decimal> GetMonthSalesAsync()
        {
            using var conn = _factory.CreateConnection();
            return await conn.ExecuteScalarAsync<decimal>(
                @"SELECT COALESCE(SUM(SubTotal), 0) FROM Invoices
                  WHERE MONTH(InvoiceDate) = MONTH(CURDATE()) AND YEAR(InvoiceDate) = YEAR(CURDATE()) AND IsCancelled = 0");
        }

        // chart: x = month(1-12), y = earnings, optionally filtered to one product
        public async Task<List<(int Month, decimal Total)>> GetMonthlyEarningsAsync(int year, int? productId = null)
        {
            using var conn = _factory.CreateConnection();
            var sql = productId.HasValue
                ? @"SELECT MONTH(i.InvoiceDate) AS Month, SUM(ii.Amount) AS Total
                    FROM InvoiceItems ii JOIN Invoices i ON i.Id = ii.InvoiceId
                    WHERE YEAR(i.InvoiceDate) = @year AND i.IsCancelled = 0 AND ii.ProductId = @productId
                    GROUP BY MONTH(i.InvoiceDate)"
                : @"SELECT MONTH(InvoiceDate) AS Month, SUM(SubTotal) AS Total
                    FROM Invoices WHERE YEAR(InvoiceDate) = @year AND IsCancelled = 0
                    GROUP BY MONTH(InvoiceDate)";

            var rows = await conn.QueryAsync<(int Month, decimal Total)>(sql, new { year, productId });
            var byMonth = rows.ToDictionary(r => r.Month, r => r.Total);
            return Enumerable.Range(1, 12).Select(m => (m, byMonth.TryGetValue(m, out var t) ? t : 0m)).ToList();
        }

        public async Task<List<(DateTime WeekStart, decimal Total)>> GetWeeklyEarningsAsync(DateTime from, DateTime to)
        {
            using var conn = _factory.CreateConnection();
            var rows = await conn.QueryAsync<(DateTime WeekStart, decimal Total)>(
                @"SELECT DATE(DATE_SUB(InvoiceDate, INTERVAL WEEKDAY(InvoiceDate) DAY)) AS WeekStart, SUM(SubTotal) AS Total
                  FROM Invoices
                  WHERE InvoiceDate BETWEEN @from AND @to AND IsCancelled = 0
                  GROUP BY WeekStart ORDER BY WeekStart",
                new { from, to });
            return rows.ToList();
        }

        public async Task<List<(int Year, decimal Total)>> GetYearlyEarningsAsync()
        {
            using var conn = _factory.CreateConnection();
            var rows = await conn.QueryAsync<(int Year, decimal Total)>(
                @"SELECT YEAR(InvoiceDate) AS Year, SUM(SubTotal) AS Total
                  FROM Invoices WHERE IsCancelled = 0
                  GROUP BY YEAR(InvoiceDate) ORDER BY Year");
            return rows.ToList();
        }
    }
}
