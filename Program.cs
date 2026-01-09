using System;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Azure.Functions.Worker.Configuration;
using Npgsql;

namespace AccountsFunction
{
 public class Program
 {
 public static void Main()
 {
 var host = new HostBuilder()
 .ConfigureAppConfiguration((context, builder) =>
 {
 // local.settings.json is optional in production but helpful for local development
 builder.AddJsonFile("local.settings.json", optional: true, reloadOnChange: true)
 .AddEnvironmentVariables();
 })
 .ConfigureFunctionsWorkerDefaults()
 .ConfigureServices((context, services) =>
 {
 var config = context.Configuration;
 // Connection string key must be PostgresConnectionString
 var connString = config.GetConnectionString("PostgresConnectionString") ?? config["PostgresConnectionString"];
 if (string.IsNullOrWhiteSpace(connString))
 throw new InvalidOperationException("PostgresConnectionString must be provided via configuration.");

 var builder = new NpgsqlDataSourceBuilder(connString);
 // NpgsqlDataSourceBuilder supports pooling by default; customize if needed
 var dataSource = builder.Build();

 services.AddSingleton(dataSource);
 services.AddSingleton<Helpers.DatabaseHelper, Helpers.DatabaseHelper>();
 })
 .Build();

 host.Run();
 }
 }
}
