using System;
using Npgsql;

namespace TravelCardFunctionApp.Helpers
{
    public static class ConnectionHelper
    {
        public static NpgsqlDataSource BuildDataSource(string connectionString)
        {
            if (string.IsNullOrWhiteSpace(connectionString))
                throw new ArgumentException("Connection string is required", nameof(connectionString));

            var builder = new NpgsqlDataSourceBuilder(connectionString);
            return builder.Build();
        }
    }
}
