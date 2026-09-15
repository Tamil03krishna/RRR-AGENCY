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
                $@"SELECT i.*, c.Name AS CustomerName, c.Phone AS CustomerPhone, pm.Name AS PaymentModeName
                   FROM Invoices i
                   JOIN Customers c ON c.Id = i.CustomerId
                   LEFT JOIN PaymentModes pm ON pm.Id = i.PaymentModeId
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

                    await _stockRepository.ReduceStockAsync(conn, tx, item.ProductId, item.Qty);
                }

                
                var remainingPayment = invoice.PaidAmount;

                if (remainingPayment > 0 && invoice.PreviousBalance > 0)
                {
                    var oldBalanceToClear = Math.Min(remainingPayment, invoice.PreviousBalance);

                    var oldInvoices = (await conn.QueryAsync<Invoice>(
                        @"SELECT * FROM Invoices
                          WHERE CustomerId = @customerId AND Id != @invoiceId
                                AND IsCancelled = 0 AND BalanceAmount > 0
                          ORDER BY InvoiceDate ASC, Id ASC
                          FOR UPDATE",
                        new { customerId = invoice.CustomerId, invoiceId }, tx)).ToList();

                    foreach (var old in oldInvoices)
                    {
                        if (oldBalanceToClear <= 0) break;

                        var applyAmount = Math.Min(oldBalanceToClear, old.BalanceAmount);
                        if (applyAmount <= 0) continue;

                        await conn.ExecuteAsync(
                            @"INSERT INTO InvoicePayments (InvoiceId, CustomerId, Amount, PaymentModeId, PaymentDate)
                              VALUES (@oldInvoiceId, @customerId, @amount, @paymentModeId, NOW())",
                            new { oldInvoiceId = old.Id, customerId = invoice.CustomerId, amount = applyAmount, paymentModeId = invoice.PaymentModeId },
                            tx);

                        await conn.ExecuteAsync(
                            @"UPDATE Invoices
                              SET PaidAmount = PaidAmount + @amount,
                                  BalanceAmount = BalanceAmount - @amount,
                                  PaymentStatus = CASE WHEN BalanceAmount - @amount <= 0 THEN 'Paid'
                                                       WHEN PaidAmount + @amount > 0 THEN 'Partial'
                                                       ELSE 'Unpaid' END
                              WHERE Id = @oldInvoiceId",
                            new { amount = applyAmount, oldInvoiceId = old.Id }, tx);

                        oldBalanceToClear -= applyAmount;
                        remainingPayment -= applyAmount;
                    }
                }

                if (remainingPayment > 0)
                {
                    await conn.ExecuteAsync(
                        @"INSERT INTO InvoicePayments (InvoiceId, CustomerId, Amount, PaymentModeId, PaymentDate)
                          VALUES (@invoiceId, @customerId, @amount, @paymentModeId, NOW())",
                        new { invoiceId, customerId = invoice.CustomerId, amount = remainingPayment, paymentModeId = invoice.PaymentModeId },
                        tx);
                }

               
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

 
        public async Task UpdateInvoiceAsync(int invoiceId, List<InvoiceItem> items, decimal paidAmount, int paymentModeId)
        {
            using var conn = (MySqlConnection)_factory.CreateConnection();
            using var tx = await conn.BeginTransactionAsync();
            try
            {
                var invoice = await conn.QueryFirstOrDefaultAsync<Invoice>(
                    "SELECT * FROM Invoices WHERE Id = @invoiceId FOR UPDATE", new { invoiceId }, tx);
                if (invoice is null) throw new InvalidOperationException("Invoice not found.");
                if (invoice.IsCancelled) throw new InvalidOperationException("Cancelled invoice-a edit panna mudiyathu.");
                if (items is null || items.Count == 0) throw new InvalidOperationException("Kammiya oru product venum bill pananum.");

                var oldItems = (await conn.QueryAsync<InvoiceItem>(
                    "SELECT * FROM InvoiceItems WHERE InvoiceId = @invoiceId", new { invoiceId }, tx)).ToList();

                foreach (var old in oldItems)
                {
                    await _stockRepository.IncreaseStockAsync(conn, tx, old.ProductId, old.Qty);
                }

                await conn.ExecuteAsync("DELETE FROM InvoiceItems WHERE InvoiceId = @invoiceId", new { invoiceId }, tx);

                foreach (var item in items)
                {
                    await _stockRepository.ReduceStockAsync(conn, tx, item.ProductId, item.Qty);

                    await conn.ExecuteAsync(
                        @"INSERT INTO InvoiceItems (InvoiceId, ProductId, PriceType, UnitPrice, Qty, Amount)
                          VALUES (@invoiceId, @ProductId, @PriceType, @UnitPrice, @Qty, @Amount)",
                        new { invoiceId, item.ProductId, item.PriceType, item.UnitPrice, item.Qty, Amount = item.UnitPrice * item.Qty },
                        tx);
                }

                var newSubTotal = items.Sum(i => i.UnitPrice * i.Qty);
                var newGrandTotal = newSubTotal + invoice.PreviousBalance;
                var newBalance = newGrandTotal - paidAmount;
                var newStatus = newBalance < 0 ? "Advance" : (newBalance == 0 ? "Paid" : (paidAmount > 0 ? "Partial" : "Unpaid"));

                var oldBalance = invoice.BalanceAmount;

                await conn.ExecuteAsync(
                    @"UPDATE Invoices
                      SET SubTotal = @newSubTotal, GrandTotal = @newGrandTotal, PaidAmount = @paidAmount,
                          PaymentModeId = @paymentModeId, BalanceAmount = @newBalance, PaymentStatus = @newStatus
                      WHERE Id = @invoiceId",
                    new { newSubTotal, newGrandTotal, paidAmount, paymentModeId, newBalance, newStatus, invoiceId }, tx);

                var delta = newBalance - oldBalance;
                if (delta != 0)
                {
                    await conn.ExecuteAsync(
                        "UPDATE Customers SET OutstandingBalance = OutstandingBalance + @delta, UpdatedDate = NOW() WHERE Id = @customerId",
                        new { delta, customerId = invoice.CustomerId }, tx);
                }

                await tx.CommitAsync();
            }
            catch
            {
                await tx.RollbackAsync();
                throw;
            }
        }


        public async Task CancelInvoiceAsync(int invoiceId)
        {
            using var conn = (MySqlConnection)_factory.CreateConnection();
            using var tx = await conn.BeginTransactionAsync();
            try
            {
                var invoice = await conn.QueryFirstOrDefaultAsync<Invoice>(
                    "SELECT * FROM Invoices WHERE Id = @invoiceId FOR UPDATE", new { invoiceId }, tx);
                if (invoice is null) throw new InvalidOperationException("Invoice not found.");
                if (invoice.IsCancelled) throw new InvalidOperationException("Invoice is already cancelled.");

                var newerInvoiceExists = await conn.ExecuteScalarAsync<bool>(
                    @"SELECT COUNT(*) > 0 FROM Invoices
                      WHERE CustomerId = @customerId AND IsCancelled = 0 AND Id != @invoiceId
                            AND (InvoiceDate > @invoiceDate OR (InvoiceDate = @invoiceDate AND Id > @invoiceId))",
                    new { customerId = invoice.CustomerId, invoiceId, invoiceDate = invoice.InvoiceDate }, tx);

                if (newerInvoiceExists)
                    throw new InvalidOperationException(
                        "This isn't the customer's latest invoice — a newer invoice already carried its balance forward. Cancel the newer invoice(s) first.");

                var items = (await conn.QueryAsync<InvoiceItem>(
                    "SELECT * FROM InvoiceItems WHERE InvoiceId = @invoiceId", new { invoiceId }, tx)).ToList();

                foreach (var item in items)
                {
                    await _stockRepository.IncreaseStockAsync(conn, tx, item.ProductId, item.Qty);
                }

       
                await conn.ExecuteAsync(
                    "DELETE FROM InvoicePayments WHERE InvoiceId = @invoiceId", new { invoiceId }, tx);

                await conn.ExecuteAsync(
                    "UPDATE Invoices SET IsCancelled = 1, BalanceAmount = 0, PaymentStatus = 'Cancelled' WHERE Id = @invoiceId",
                    new { invoiceId }, tx);

                if (invoice.BalanceAmount != 0)
                {
           
                    await conn.ExecuteAsync(
                        "UPDATE Customers SET OutstandingBalance = OutstandingBalance - @balance, UpdatedDate = NOW() WHERE Id = @customerId",
                        new { balance = invoice.BalanceAmount, customerId = invoice.CustomerId }, tx);
                }

                await tx.CommitAsync();
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


        public async Task<List<MonthlyEarningDto>> GetMonthlyEarningsWithProfitAsync(int year, int? productId = null)
        {
            using var conn = _factory.CreateConnection();

            var sql = @"
        SELECT 
            MONTH(i.InvoiceDate) AS Month,
            COALESCE(SUM(ii.Amount), 0) AS Revenue,
            COALESCE(SUM(ii.Qty * (p.MrpPrice - p.StorePrice)), 0) AS Profit,
            COUNT(DISTINCT i.Id) AS Count
        FROM Invoices i
        INNER JOIN InvoiceItems ii ON ii.InvoiceId = i.Id
        INNER JOIN Products p ON p.Id = ii.ProductId
        WHERE YEAR(i.InvoiceDate) = @year 
            AND i.IsCancelled = 0";

            if (productId.HasValue)
            {
                sql += " AND ii.ProductId = @productId";
            }

            sql += " GROUP BY MONTH(i.InvoiceDate)";

            var rows = await conn.QueryAsync<MonthlyEarningDto>(sql, new { year, productId });
            var result = rows.ToList();

            var allMonths = Enumerable.Range(1, 12)
                .Select(m => new MonthlyEarningDto
                {
                    Month = m,
                    Revenue = result.FirstOrDefault(r => r.Month == m)?.Revenue ?? 0,
                    Profit = result.FirstOrDefault(r => r.Month == m)?.Profit ?? 0,
                    Count = result.FirstOrDefault(r => r.Month == m)?.Count ?? 0
                })
                .ToList();

            return allMonths;
        }

        public async Task<List<WeeklyEarningDto>> GetWeeklyEarningsWithProfitAsync(int weeks, int? productId = null)
        {
            using var conn = _factory.CreateConnection();

            var fromDate = DateTime.Today.AddDays(-(weeks * 7));

            var sql = @"
        SELECT 
            DATE(DATE_SUB(i.InvoiceDate, INTERVAL WEEKDAY(i.InvoiceDate) DAY)) AS WeekStart,
            COALESCE(SUM(ii.Amount), 0) AS Revenue,
            COALESCE(SUM(ii.Qty * (p.MrpPrice - p.StorePrice)), 0) AS Profit,
            COUNT(DISTINCT i.Id) AS Count
        FROM Invoices i
        INNER JOIN InvoiceItems ii ON ii.InvoiceId = i.Id
        INNER JOIN Products p ON p.Id = ii.ProductId
        WHERE i.InvoiceDate >= @fromDate 
            AND i.IsCancelled = 0";

            if (productId.HasValue)
            {
                sql += " AND ii.ProductId = @productId";
            }

            sql += " GROUP BY WeekStart ORDER BY WeekStart";

            var rows = await conn.QueryAsync<WeeklyEarningDto>(sql, new { fromDate, productId });
            return rows.ToList();
        }

        public async Task<List<YearlyEarningDto>> GetYearlyEarningsWithProfitAsync(int? productId = null)
        {
            using var conn = _factory.CreateConnection();

            var sql = @"
        SELECT 
            YEAR(i.InvoiceDate) AS Year,
            COALESCE(SUM(ii.Amount), 0) AS Revenue,
            COALESCE(SUM(ii.Qty * (p.MrpPrice - p.StorePrice)), 0) AS Profit,
            COUNT(DISTINCT i.Id) AS Count
        FROM Invoices i
        INNER JOIN InvoiceItems ii ON ii.InvoiceId = i.Id
        INNER JOIN Products p ON p.Id = ii.ProductId
        WHERE i.IsCancelled = 0";

            if (productId.HasValue)
            {
                sql += " AND ii.ProductId = @productId";
            }

            sql += " GROUP BY YEAR(i.InvoiceDate) ORDER BY Year";

            var rows = await conn.QueryAsync<YearlyEarningDto>(sql, new { productId });
            return rows.ToList();
        }

        public async Task<List<ProductPerformanceDto>> GetProductPerformanceAsync(int year)
        {
            using var conn = _factory.CreateConnection();

            var sql = @"
        SELECT 
            p.Name AS ProductName,
            COALESCE(SUM(ii.Amount), 0) AS Revenue,
            COALESCE(SUM(ii.Qty * (p.MrpPrice - p.StorePrice)), 0) AS Profit,
            COALESCE(SUM(ii.Qty), 0) AS Quantity
        FROM Products p
        INNER JOIN InvoiceItems ii ON ii.ProductId = p.Id
        INNER JOIN Invoices i ON i.Id = ii.InvoiceId
        WHERE YEAR(i.InvoiceDate) = @year 
            AND i.IsCancelled = 0
            AND p.IsActive = 1
        GROUP BY p.Id, p.Name
        ORDER BY Revenue DESC
        LIMIT 10";

            var rows = await conn.QueryAsync<ProductPerformanceDto>(sql, new { year });
            return rows.ToList();
        }
    }
}
