using System;
using System.Threading.Tasks;
using Npgsql;

namespace TravelcardApi.Helpers
{
    public class DatabaseHelper
    {
        private readonly NpgsqlDataSource _dataSource;

        public DatabaseHelper(NpgsqlDataSource dataSource)
        {
            _dataSource = dataSource ?? throw new ArgumentNullException(nameof(dataSource));
        }

        public async Task<NpgsqlConnection> OpenConnectionAsync()
        {
            var conn = await _dataSource.OpenConnectionAsync().ConfigureAwait(false);
            return conn;
        }
    }
}
