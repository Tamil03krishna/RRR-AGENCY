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

                // BUG FIX: previously the entire PaidAmount was recorded only against this
                // new invoice, so old invoices that had a PreviousBalance carried into this
                // bill never got their own PaidAmount/BalanceAmount/PaymentStatus updated.
                // That made an old invoice stay "Partial" forever even after the customer
                // had fully paid it off through a later bill.
                // Fix: apply the paid amount FIFO — oldest outstanding invoices first — and
                // settle their own rows, then whatever remains goes toward this new invoice.
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

                // Whatever is left (after clearing old dues) is the payment towards this invoice's own bill
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

        public async Task UpdateInvoiceItemAsync(int invoiceId, int itemId, decimal qty, decimal unitPrice, string priceType)
        {
            using var conn = (MySqlConnection)_factory.CreateConnection();
            using var tx = await conn.BeginTransactionAsync();
            try
            {
                var item = await conn.QueryFirstOrDefaultAsync<InvoiceItem>(
                    "SELECT * FROM InvoiceItems WHERE Id = @itemId AND InvoiceId = @invoiceId FOR UPDATE",
                    new { itemId, invoiceId }, tx);
                if (item is null) throw new InvalidOperationException("Invoice item not found.");

                // put back the old qty, then take out the new qty (handles both increase & decrease correctly)
                await _stockRepository.IncreaseStockAsync(conn, tx, item.ProductId, item.Qty);
                await _stockRepository.ReduceStockAsync(conn, tx, item.ProductId, qty);

                var amount = unitPrice * qty;
                await conn.ExecuteAsync(
                    @"UPDATE InvoiceItems SET Qty = @qty, UnitPrice = @unitPrice, PriceType = @priceType, Amount = @amount
                      WHERE Id = @itemId",
                    new { qty, unitPrice, priceType, amount, itemId }, tx);

                await RecalculateInvoiceTotalsAsync(conn, tx, invoiceId);

                await tx.CommitAsync();
            }
            catch
            {
                await tx.RollbackAsync();
                throw;
            }
        }

        public async Task DeleteInvoiceItemAsync(int invoiceId, int itemId)
        {
            using var conn = (MySqlConnection)_factory.CreateConnection();
            using var tx = await conn.BeginTransactionAsync();
            try
            {
                var itemCount = await conn.ExecuteScalarAsync<int>(
                    "SELECT COUNT(*) FROM InvoiceItems WHERE InvoiceId = @invoiceId", new { invoiceId }, tx);
                if (itemCount <= 1)
                    throw new InvalidOperationException("Invoice-la kadaisi item-a delete panna mudiyathu. Full invoice-a cancel pannunga.");

                var item = await conn.QueryFirstOrDefaultAsync<InvoiceItem>(
                    "SELECT * FROM InvoiceItems WHERE Id = @itemId AND InvoiceId = @invoiceId FOR UPDATE",
                    new { itemId, invoiceId }, tx);
                if (item is null) throw new InvalidOperationException("Invoice item not found.");

                await _stockRepository.IncreaseStockAsync(conn, tx, item.ProductId, item.Qty);

                await conn.ExecuteAsync("DELETE FROM InvoiceItems WHERE Id = @itemId", new { itemId }, tx);

                await RecalculateInvoiceTotalsAsync(conn, tx, invoiceId);

                await tx.CommitAsync();
            }
            catch
            {
                await tx.RollbackAsync();
                throw;
            }
        }

        // Recomputes SubTotal/GrandTotal/BalanceAmount/PaymentStatus for an invoice after its
        // items changed, and keeps the customer's running OutstandingBalance in sync by delta
        // (not a flat overwrite) so it doesn't clobber balances contributed by other invoices.
        private async Task RecalculateInvoiceTotalsAsync(MySqlConnection conn, MySqlTransaction tx, int invoiceId)
        {
            var invoice = await conn.QueryFirstOrDefaultAsync<Invoice>(
                "SELECT * FROM Invoices WHERE Id = @invoiceId FOR UPDATE", new { invoiceId }, tx);
            if (invoice is null) throw new InvalidOperationException("Invoice not found.");

            var newSubTotal = await conn.ExecuteScalarAsync<decimal>(
                "SELECT COALESCE(SUM(Amount), 0) FROM InvoiceItems WHERE InvoiceId = @invoiceId", new { invoiceId }, tx);

            var newGrandTotal = newSubTotal + invoice.PreviousBalance;
            var newBalance = newGrandTotal - invoice.PaidAmount;
            if (newBalance < 0) newBalance = 0;
            var newStatus = newBalance <= 0 ? "Paid" : (invoice.PaidAmount > 0 ? "Partial" : "Unpaid");

            var oldBalance = invoice.BalanceAmount;

            await conn.ExecuteAsync(
                @"UPDATE Invoices
                  SET SubTotal = @newSubTotal, GrandTotal = @newGrandTotal,
                      BalanceAmount = @newBalance, PaymentStatus = @newStatus
                  WHERE Id = @invoiceId",
                new { newSubTotal, newGrandTotal, newBalance, newStatus, invoiceId }, tx);

            var delta = newBalance - oldBalance;
            if (delta != 0)
            {
                await conn.ExecuteAsync(
                    "UPDATE Customers SET OutstandingBalance = OutstandingBalance + @delta, UpdatedDate = NOW() WHERE Id = @customerId",
                    new { delta, customerId = invoice.CustomerId }, tx);
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

        // Add these methods to InvoiceRepository class

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

            // Fill missing months with zero values
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