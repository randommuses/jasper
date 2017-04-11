using System;
using Npgsql;

namespace JasperBus.Marten.Tests.Setup
{
    public interface IConnectionFactory
    {
        /// <summary>
        /// Create a new, isolated connection to the Postgresql database
        /// </summary>
        /// <returns></returns>
        NpgsqlConnection Create();
    }

    public class ConnectionFactory : IConnectionFactory
    {
        private readonly Lazy<string> _connectionString;

        /// <summary>
        /// Supply a lambda that can resolve the connection string
        /// for a Postgresql database
        /// </summary>
        /// <param name="connectionSource"></param>
        public ConnectionFactory(Func<string> connectionSource)
        {
            _connectionString = new Lazy<string>(connectionSource);
        }

        /// <summary>
        /// Supply the connection string to the Postgresql database directly
        /// </summary>
        /// <param name="connectionString"></param>
        public ConnectionFactory(string connectionString)
        {
            _connectionString = new Lazy<string>(() => connectionString);
        }

        public NpgsqlConnection Create()
        {
            return new NpgsqlConnection(_connectionString.Value);
        }
    }
}
