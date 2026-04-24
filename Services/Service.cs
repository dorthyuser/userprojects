using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Threading.Tasks;
using Npgsql;
using NpgsqlTypes;
using UpTransportTicketApiLambda.Models;

namespace UpTransportTicketApiLambda.Services
{
    public class Service
    {
        private readonly string _connString;
        private readonly NpgsqlDataSource _dataSource;
        public static readonly JsonSerializerOptions JsonOptions = new JsonSerializerOptions
        {
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
            Converters = { new JsonStringEnumConverter(JsonNamingPolicy.CamelCase) }
        };

        private const decimal BasePrice = 50m; // rs 50

        public Service(string connectionString)
        {
            _connString = connectionString ?? throw new ArgumentNullException(nameof(connectionString));

            Console.WriteLine("Initializing NpgsqlDataSource builder...");
            var builder = new NpgsqlDataSourceBuilder(_connString);

            // Map enum to DB enum name with exact translator
            builder.MapEnum<Category>("category_enum", new UpTransportTicketApiLambda.Models.ExactNameTranslator());

            _dataSource = builder.Build();

            // Warm up a connection to initiate pool
            Task.Run(async () =>
            {
                try
                {
                    Console.WriteLine("Warming up connection pool...");
                    await using var conn = await _dataSource.OpenConnectionAsync();
                    Console.WriteLine("Connection pool initialized.");
                    await conn.CloseAsync();
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"Failed to warm up DB connection: {ex}");
                }
            }).Wait();
        }

        // Validation method
        private void ValidateRequest(Request request)
        {
            if (request == null) throw new ArgumentNullException(nameof(request));
            if (string.IsNullOrWhiteSpace(request.Name)) throw new ArgumentException("Name is required");
            if (request.Categories == null || request.Categories.Count == 0) throw new ArgumentException("At least one category is required");
        }

        // Calculate highest applicable discount when multiple categories present
        private static decimal CalculateDiscountPercent(IEnumerable<Category> categories)
        {
            // student - 50%, business-man - 5%, employee - 5%, staff -25%
            decimal best = 0m;
            foreach (var c in categories)
            {
                var pct = c switch
                {
                    Category.Student => 50m,
                    Category.BusinessMan => 5m,
                    Category.Employee => 5m,
                    Category.Staff => 25m,
                    _ => 0m
                };
                if (pct > best) best = pct;
            }
            return best;
        }

        public async Task<Response> CreateTicketAsync(Request request)
        {
            try
            {
                Console.WriteLine("CreateTicketAsync called");
                ValidateRequest(request);

                var id = Guid.NewGuid();
                var createdAt = DateTimeOffset.UtcNow;
                var price = BasePrice;
                var discountPercent = CalculateDiscountPercent(request.Categories);
                var discounted = Math.Round(price - (price * (discountPercent / 100m)), 2);

                await using var conn = await _dataSource.OpenConnectionAsync();
                await using var cmd = conn.CreateCommand();
                cmd.CommandText = "INSERT INTO tickets (id, name, categories, price, discounted_price, expired, created_at) VALUES (@id, @name, @categories, @price, @discounted_price, @expired, @created_at)";

                var pId = cmd.Parameters.Add("@id", NpgsqlDbType.Uuid);
                pId.Value = id;

                var pName = cmd.Parameters.Add("@name", NpgsqlDbType.Text);
                pName.Value = request.Name;

                // store categories as text[] in DB
                var catArray = request.Categories.Select(c => CategoryToString(c)).ToArray();
                var pCats = cmd.Parameters.Add("@categories", NpgsqlDbType.Array | NpgsqlDbType.Text);
                pCats.Value = catArray;

                var pPrice = cmd.Parameters.Add("@price", NpgsqlDbType.Numeric);
                pPrice.Value = price;

                var pDisc = cmd.Parameters.Add("@discounted_price", NpgsqlDbType.Numeric);
                pDisc.Value = discounted;

                var pExpired = cmd.Parameters.Add("@expired", NpgsqlDbType.Boolean);
                pExpired.Value = false;

                var pCreated = cmd.Parameters.Add("@created_at", NpgsqlDbType.TimestampTz);
                pCreated.Value = createdAt;

                var rows = await cmd.ExecuteNonQueryAsync();
                Console.WriteLine($"Inserted rows: {rows}");

                return new Response
                {
                    Id = id,
                    Name = request.Name,
                    Categories = request.Categories.ToList(),
                    Price = price,
                    DiscountedPrice = discounted,
                    Expired = false,
                    CreatedAt = createdAt
                };
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error in CreateTicketAsync: {ex}");
                throw;
            }
        }

        public async Task<Response?> GetTicketAsync(Guid id)
        {
            try
            {
                Console.WriteLine($"GetTicketAsync for id={id}");
                await using var conn = await _dataSource.OpenConnectionAsync();
                await using var cmd = conn.CreateCommand();
                cmd.CommandText = "SELECT id, name, categories, price, discounted_price, expired, created_at FROM tickets WHERE id = @id";
                var pId = cmd.Parameters.Add("@id", NpgsqlDbType.Uuid);
                pId.Value = id;

                await using var reader = await cmd.ExecuteReaderAsync();
                if (!await reader.ReadAsync()) return null;

                var res = new Response
                {
                    Id = reader.GetGuid(0),
                    Name = reader.GetString(1),
                    Categories = reader.GetFieldValue<string[]>(2).Select(s => StringToCategory(s)).ToList(),
                    Price = reader.GetDecimal(3),
                    DiscountedPrice = reader.GetDecimal(4),
                    Expired = reader.GetBoolean(5),
                    CreatedAt = reader.GetFieldValue<DateTimeOffset>(6)
                };

                return res;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error in GetTicketAsync: {ex}");
                throw;
            }
        }

        public async Task<Response?> UpdateTicketAsync(Guid id, Request request)
        {
            try
            {
                Console.WriteLine($"UpdateTicketAsync id={id}");
                ValidateRequest(request);

                var discountPercent = CalculateDiscountPercent(request.Categories);
                var discounted = Math.Round(BasePrice - (BasePrice * (discountPercent / 100m)), 2);

                await using var conn = await _dataSource.OpenConnectionAsync();
                await using var cmd = conn.CreateCommand();
                cmd.CommandText = "UPDATE tickets SET name=@name, categories=@categories, discounted_price=@discounted_price WHERE id=@id";

                var pName = cmd.Parameters.Add("@name", NpgsqlDbType.Text);
                pName.Value = request.Name;

                var catArray = request.Categories.Select(c => CategoryToString(c)).ToArray();
                var pCats = cmd.Parameters.Add("@categories", NpgsqlDbType.Array | NpgsqlDbType.Text);
                pCats.Value = catArray;

                var pDisc = cmd.Parameters.Add("@discounted_price", NpgsqlDbType.Numeric);
                pDisc.Value = discounted;

                var pId = cmd.Parameters.Add("@id", NpgsqlDbType.Uuid);
                pId.Value = id;

                var rows = await cmd.ExecuteNonQueryAsync();
                if (rows == 0) return null;

                return await GetTicketAsync(id);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error in UpdateTicketAsync: {ex}");
                throw;
            }
        }

        public async Task<Response?> ExpireTicketAsync(Guid id)
        {
            try
            {
                Console.WriteLine($"ExpireTicketAsync id={id}");
                await using var conn = await _dataSource.OpenConnectionAsync();
                await using var cmd = conn.CreateCommand();
                cmd.CommandText = "UPDATE tickets SET expired = true WHERE id = @id";
                var pId = cmd.Parameters.Add("@id", NpgsqlDbType.Uuid);
                pId.Value = id;
                var rows = await cmd.ExecuteNonQueryAsync();
                if (rows == 0) return null;
                return await GetTicketAsync(id);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error in ExpireTicketAsync: {ex}");
                throw;
            }
        }

        public async Task<bool> DeleteTicketAsync(Guid id)
        {
            try
            {
                Console.WriteLine($"DeleteTicketAsync id={id}");
                await using var conn = await _dataSource.OpenConnectionAsync();
                await using var cmd = conn.CreateCommand();
                cmd.CommandText = "DELETE FROM tickets WHERE id = @id";
                var pId = cmd.Parameters.Add("@id", NpgsqlDbType.Uuid);
                pId.Value = id;
                var rows = await cmd.ExecuteNonQueryAsync();
                return rows > 0;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error in DeleteTicketAsync: {ex}");
                throw;
            }
        }

        private static string CategoryToString(Category c)
        {
            return c switch
            {
                Category.Student => "student",
                Category.BusinessMan => "business-man",
                Category.Employee => "employee",
                Category.Staff => "staff",
                _ => c.ToString()
            };
        }

        private static Category StringToCategory(string s)
        {
            return s switch
            {
                "student" => Category.Student,
                "business-man" => Category.BusinessMan,
                "employee" => Category.Employee,
                "staff" => Category.Staff,
                _ => throw new ArgumentException($"Unknown category string: {s}")
            };
        }
    }
}
