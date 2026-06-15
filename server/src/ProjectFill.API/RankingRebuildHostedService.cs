using ProjectFill.Application.Ranking;

namespace ProjectFill.API;

public sealed class RankingRebuildHostedService : BackgroundService
{
    private readonly IServiceScopeFactory _scopeFactory;
    private readonly ILogger<RankingRebuildHostedService> _logger;

    public RankingRebuildHostedService(IServiceScopeFactory scopeFactory, ILogger<RankingRebuildHostedService> logger)
    {
        _scopeFactory = scopeFactory;
        _logger = logger;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        try
        {
            using var scope = _scopeFactory.CreateScope();
            var ranking = scope.ServiceProvider.GetRequiredService<RankingService>();
            await ranking.RebuildAllAsync(stoppingToken);
        }
        catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested)
        {
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Ranking Redis rebuild failed during server init.");
        }
    }
}
