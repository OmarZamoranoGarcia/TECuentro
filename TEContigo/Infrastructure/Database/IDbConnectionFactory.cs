using System.Data;

namespace TEContigo.Infrastructure.Database
{
    public interface IDbConnectionFactory
    {
        IDbConnection CreateConnection();
    }
}
