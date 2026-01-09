using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Npgsql;
using Microsoft.Extensions.Logging;
using AccountsFunction.Models;

namespace AccountsFunction.Helpers
{
 public class DatabaseHelper
 {
 private readonly NpgsqlDataSource _dataSource;
 private readonly ILogger<DatabaseHelper> _logger;

 public DatabaseHelper(NpgsqlDataSource dataSource, ILogger<DatabaseHelper> logger)
 {
 _dataSource = dataSource ?? throw new ArgumentNullException(nameof(dataSource));
 _logger = logger;
 }

 public async Task<Account?> GetAccountByIdAsync(Guid id)
 {
 await using var conn = await _dataSource.OpenConnectionAsync();
 await using var cmd = conn.CreateCommand();
 cmd.CommandText = "SELECT id, name, email, address FROM accounts WHERE id = @id";
 cmd.Parameters.AddWithValue("id", id);

 try
 {
 await using var reader = await cmd.ExecuteReaderAsync();
 if (await reader.ReadAsync())
 {
 var account = new Account
 {
 Id = reader.GetFieldValue<Guid>(0),
 Name = reader.IsDBNull(1) ? string.Empty : reader.GetString(1),
 Email = reader.IsDBNull(2) ? string.Empty : reader.GetString(2),
 Address = reader.IsDBNull(3) ? string.Empty : reader.GetString(3)
 };
 return account;
 }
 return null;
 }
 catch (Exception ex)
 {
 _logger?.LogError(ex, "Error while fetching account by id {Id}", id);
 throw;
 }
 }

 public async Task<List<Account>> GetAllAccountsAsync()
 {
 var result = new List<Account>();
 await using var conn = await _dataSource.OpenConnectionAsync();
 await using var cmd = conn.CreateCommand();
 cmd.CommandText = "SELECT id, name, email, address FROM accounts ORDER BY name";

 try
 {
 await using var reader = await cmd.ExecuteReaderAsync();
 while (await reader.ReadAsync())
 {
 var account = new Account
 {
 Id = reader.GetFieldValue<Guid>(0),
 Name = reader.IsDBNull(1) ? string.Empty : reader.GetString(1),
 Email = reader.IsDBNull(2) ? string.Empty : reader.GetString(2),
 Address = reader.IsDBNull(3) ? string.Empty : reader.GetString(3)
 };
 result.Add(account);
 }
 return result;
 }
 catch (Exception ex)
 {
 _logger?.LogError(ex, "Error while fetching all accounts");
 throw;
 }
 }

 // Validation helpers
 public static bool TryParseGuid(string? value, out Guid id)
 {
 if (string.IsNullOrWhiteSpace(value))
 {
 id = Guid.Empty;
 return false;
 }
 return Guid.TryParse(value, out id);
 }
 }
}
