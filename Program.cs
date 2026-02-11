using System;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.EntityFrameworkCore;
using Microsoft.Azure.Functions.Worker.Configuration;
using SalesforceAccountFunctions.Helpers.Salesforce;
using SalesforceAccountFunctions.Data;

var host = new HostBuilder()
    .ConfigureFunctionsWorkerDefaults()
    .ConfigureAppConfiguration((context, builder) =>
    {
        builder.AddEnvironmentVariables();
    })
    .ConfigureServices((context, services) =>
    {
        var configuration = context.Configuration;

        services.AddHttpClient("salesforce")
            .ConfigureHttpClient((sp, client) =>
            {
                // Base address set in SalesforceClient when needed from SF_INSTANCE_URL
            });

        services.AddScoped<ISalesforceClient, SalesforceClient>();

        var sqlConn = configuration["SQL_CONNECTION_STRING"];
        if (!string.IsNullOrEmpty(sqlConn))
        {
            services.AddDbContext<SalesforceDbContext>(options =>
            {
                options.UseSqlServer(sqlConn);
            });
        }

    })
    .ConfigureLogging(logging =>
    {
        logging.AddConsole();
    })
    .Build();

host.Run();