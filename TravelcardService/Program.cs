using System;
using System.Threading.Tasks;
using Azure.Identity;
using Azure.Security.KeyVault.Secrets;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using TravelcardService.Helpers;

namespace TravelcardService
{
    public class Program
    {
        public static async Task Main(string[] args)
        {
            var kvUri = Environment.GetEnvironmentVariable("KEY_VAULT_URI");
            if (string.IsNullOrWhiteSpace(kvUri))
            {
                Console.WriteLine("Warning: KEY_VAULT_URI environment variable is not set. Key Vault access will fail until configured.");
            }

            var host = new HostBuilder()
                .ConfigureFunctionsWorkerDefaults()
                .ConfigureServices(services =>
                {
                    services.AddHttpClient();
                    services.AddSingleton<IKeyVaultHelper>((sp) =>
                    {
                        if (string.IsNullOrWhiteSpace(kvUri))
                        {
                            throw new InvalidOperationException("KEY_VAULT_URI must be set in environment variables to resolve secrets from Key Vault.");
                        }

                        var secretClient = new SecretClient(new Uri(kvUri), new DefaultAzureCredential());
                        return new KeyVaultHelper(secretClient);
                    });

                    services.AddSingleton<ITokenService, TokenService>();
                    services.AddSingleton<ITravelcardHttpHelper, TravelcardHttpHelper>();

                    services.AddLogging(loggingBuilder =>
                    {
                        loggingBuilder.AddConsole();
                    });
                })
                .Build();

            await host.RunAsync().ConfigureAwait(false);
        }
    }
}
