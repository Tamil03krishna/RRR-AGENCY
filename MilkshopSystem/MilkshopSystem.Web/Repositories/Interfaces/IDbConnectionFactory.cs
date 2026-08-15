using System.Data;

namespace MilkshopSystem.Web.Repositories.Interfaces
{
    /// <summary>
    /// Factory that hands out a fresh, open ADO.NET connection for Dapper to use.
    /// Every repository method should: using var conn = _factory.CreateConnection();
    /// </summary>
    public interface IDbConnectionFactory
    {
        IDbConnection CreateConnection();
    }
}
