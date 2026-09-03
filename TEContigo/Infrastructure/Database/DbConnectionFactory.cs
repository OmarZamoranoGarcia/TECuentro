using System.Data;
using Npgsql;

namespace TEContigo.Infrastructure.Database
{
    public class DbConnectionFactory : IDbConnectionFactory
    {
        private readonly string _connectionString;

        public DbConnectionFactory(IConfiguration configuration)
        {
            _connectionString = configuration.GetConnectionString(
                "DefaultConnection"
            ) ?? throw new InvalidOperationException(
                "La cadena de conexión 'DefaultConnection' no está configurada."
            );
        }

        public IDbConnection CreateConnection()
        {
            return new NpgsqlConnection(_connectionString);
        }
    }
}
