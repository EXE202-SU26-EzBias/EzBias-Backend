namespace EzBias.API.BackgroundServices;

/// <summary>
/// Background service that pings a configured API endpoint every 4 minutes
/// to prevent Render free tier from spinning down due to inactivity.
/// </summary>
public class KeepAliveScheduler : BackgroundService
{
    private readonly IHttpClientFactory _httpClientFactory;
    private readonly ILogger<KeepAliveScheduler> _logger;
    private readonly IConfiguration _config;

    public KeepAliveScheduler(
        IHttpClientFactory httpClientFactory,
        ILogger<KeepAliveScheduler> logger,
        IConfiguration config)
    {
        _httpClientFactory = httpClientFactory;
        _logger = logger;
        _config = config;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        var enabled = _config.GetValue<bool?>("KeepAlive:Enabled") ?? false;
        if (!enabled)
        {
            _logger.LogInformation("KeepAliveScheduler is disabled in configuration.");
            return;
        }

        var targetUrl = _config.GetValue<string?>("KeepAlive:TargetUrl");
        if (string.IsNullOrWhiteSpace(targetUrl))
        {
            _logger.LogWarning("KeepAliveScheduler enabled but TargetUrl not configured. Service will not start.");
            return;
        }

        var intervalSeconds = _config.GetValue<int?>("KeepAlive:IntervalSeconds") ?? 240; // Default 4 minutes
        if (intervalSeconds < 30) intervalSeconds = 30; // Minimum 30 seconds to avoid too frequent requests

        _logger.LogInformation(
            "KeepAliveScheduler started. Target={TargetUrl}, Interval={IntervalSeconds}s ({Minutes}min)",
            targetUrl,
            intervalSeconds,
            intervalSeconds / 60.0);

        // Wait a bit before first ping to allow app to fully start
        await Task.Delay(TimeSpan.FromSeconds(10), stoppingToken);

        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                var client = _httpClientFactory.CreateClient("KeepAlive");
                client.Timeout = TimeSpan.FromSeconds(30);

                var sw = System.Diagnostics.Stopwatch.StartNew();
                var response = await client.GetAsync(targetUrl, stoppingToken);
                sw.Stop();

                if (response.IsSuccessStatusCode)
                {
                    _logger.LogInformation(
                        "KeepAlive ping succeeded: {StatusCode} in {ElapsedMs}ms",
                        (int)response.StatusCode,
                        sw.ElapsedMilliseconds);
                }
                else
                {
                    _logger.LogWarning(
                        "KeepAlive ping returned non-success status: {StatusCode} in {ElapsedMs}ms",
                        (int)response.StatusCode,
                        sw.ElapsedMilliseconds);
                }
            }
            catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested)
            {
                break;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "KeepAlive ping failed: {Message}", ex.Message);
            }

            await Task.Delay(TimeSpan.FromSeconds(intervalSeconds), stoppingToken);
        }

        _logger.LogInformation("KeepAliveScheduler stopped.");
    }
}
