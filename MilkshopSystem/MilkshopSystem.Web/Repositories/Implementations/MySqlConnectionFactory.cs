using System.Data;
using MilkshopSystem.Web.Repositories.Interfaces;
using MySqlConnector;

namespace MilkshopSystem.Web.Repositories.Implementations
{
    public class MySqlConnectionFactory : IDbConnectionFactory
    {
        private readonly string _connectionString;

        public MySqlConnectionFactory(string connectionString)
        {
            _connectionString = connectionString;
        }

        public IDbConnection CreateConnection()
        {
            var connection = new MySqlConnection(_connectionString);
            connection.Open();
            return connection;
        }
    }
}
