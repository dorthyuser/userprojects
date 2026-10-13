namespace BusTravelAccountsSaLambda;

public sealed class HealthService
{
    private readonly PostgresClient client;

    public HealthService(Settings settings)
    {
        client = new PostgresClient(settings.Database);
    }

    public Task<APIGatewayProxyResponse> HandleAliveAsync(string method)
    {
        return Task.FromResult(ResponseFactory.Text(200, "UP"));
    }

    public async Task<APIGatewayProxyResponse> HandleReadyAsync(string method)
    {
        try
        {
            var ok = await client.CanConnectAsync(CancellationToken.None);
            return ok ? ResponseFactory.Text(200, "UP") : ResponseFactory.Text(500, "DOWN");
        }
        catch (Exception ex)
        {
            AppLog.Error(ex.Message, ex);
            return ResponseFactory.Text(500, "DOWN");
        }
    }
}
