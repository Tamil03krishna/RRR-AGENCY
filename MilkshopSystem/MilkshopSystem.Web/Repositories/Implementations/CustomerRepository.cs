using Dapper;
using MilkshopSystem.Web.Models.Entities;
using MilkshopSystem.Web.Models.ViewModels;
using MilkshopSystem.Web.Repositories.Interfaces;

namespace MilkshopSystem.Web.Repositories.Implementations
{
    public class CustomerRepository : ICustomerRepository
    {
        private readonly IDbConnectionFactory _factory;
        public CustomerRepository(IDbConnectionFactory factory) => _factory = factory;

        public async Task<PagedResult<Customer>> GetPagedAsync(string? search, int pageNumber, int pageSize)
        {
            using var conn = _factory.CreateConnection();
            var where = string.IsNullOrWhiteSpace(search)
                ? "WHERE IsActive = 1"
                : "WHERE IsActive = 1 AND (Name LIKE @search OR Phone LIKE @search)";
            var searchParam = $"%{search}%";

            var total = await conn.ExecuteScalarAsync<int>($"SELECT COUNT(*) FROM Customers {where}", new { search = searchParam });

            var items = await conn.QueryAsync<Customer>(
                $@"SELECT * FROM Customers {where}
                   ORDER BY CreatedDate DESC
                   LIMIT @pageSize OFFSET @offset",
                new { search = searchParam, pageSize, offset = (pageNumber - 1) * pageSize });

            return new PagedResult<Customer>
            {
                Items = items.ToList(), TotalRecords = total, PageNumber = pageNumber, PageSize = pageSize, SearchTerm = search
            };
        }

        public async Task<Customer?> GetByIdAsync(int id)
        {
            using var conn = _factory.CreateConnection();
            return await conn.QueryFirstOrDefaultAsync<Customer>("SELECT * FROM Customers WHERE Id = @id", new { id });
        }

        public async Task<List<Customer>> SearchAsync(string term, int maxResults = 10)
        {
            using var conn = _factory.CreateConnection();
            var result = await conn.QueryAsync<Customer>(
                @"SELECT * FROM Customers
                  WHERE IsActive = 1 AND (Name LIKE @term OR Phone LIKE @term)
                  ORDER BY Name LIMIT @maxResults",
                new { term = $"%{term}%", maxResults });
            return result.ToList();
        }

        public async Task<int> CreateAsync(Customer customer)
        {
            using var conn = _factory.CreateConnection();
            var sql = @"INSERT INTO Customers (Name, Phone, Address, Email, OutstandingBalance, IsActive, CreatedDate)
                        VALUES (@Name, @Phone, @Address, @Email, 0, 1, NOW());
                        SELECT LAST_INSERT_ID();";
            return await conn.ExecuteScalarAsync<int>(sql, customer);
        }

        public async Task<bool> UpdateAsync(Customer customer)
        {
            using var conn = _factory.CreateConnection();
            var rows = await conn.ExecuteAsync(
                @"UPDATE Customers SET Name=@Name, Phone=@Phone, Address=@Address, Email=@Email, UpdatedDate=NOW()
                  WHERE Id=@Id", customer);
            return rows > 0;
        }

        public async Task<bool> DeleteAsync(int id)
        {
            using var conn = _factory.CreateConnection();
            var rows = await conn.ExecuteAsync("UPDATE Customers SET IsActive = 0 WHERE Id = @id", new { id });
            return rows > 0;
        }

        public async Task<bool> UpdateOutstandingBalanceAsync(int customerId, decimal newBalance)
        {
            using var conn = _factory.CreateConnection();
            var rows = await conn.ExecuteAsync(
                "UPDATE Customers SET OutstandingBalance = @newBalance, UpdatedDate = NOW() WHERE Id = @customerId",
                new { customerId, newBalance });
            return rows > 0;
        }
    }
}
