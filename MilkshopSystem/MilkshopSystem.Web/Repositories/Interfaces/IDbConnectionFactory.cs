using System.Data;

namespace MilkshopSystem.Web.Repositories.Interfaces
{

    public interface IDbConnectionFactory
    {
        IDbConnection CreateConnection();
    }
}
