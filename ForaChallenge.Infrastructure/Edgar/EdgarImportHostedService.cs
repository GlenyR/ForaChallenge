using ForaChallenge.Application.Services;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace ForaChallenge.Infrastructure.Edgar;

public class EdgarImportHostedService : IHostedService
{
    private readonly IEdgarImportService _importService;
    private readonly IConfiguration _configuration;
    private readonly ILogger<EdgarImportHostedService> _logger;

    public EdgarImportHostedService(
        IEdgarImportService importService,
        IConfiguration configuration,
        ILogger<EdgarImportHostedService> logger)
    {
        _importService = importService;
        _configuration = configuration;
        _logger = logger;
    }

    public async Task StartAsync(CancellationToken cancellationToken)
    {
        var processOnStartup = _configuration.GetValue("SecEdgar:ProcessPendingCiksOnStartup", true);
        if (!processOnStartup)
        {
            _logger.LogInformation("ProcessPendingCiksOnStartup is false; skipping EDGAR import at startup.");
            return;
        }

        _logger.LogInformation("Starting EDGAR import of pending CIKs (background).");
        try
        {
            var result = await _importService.ProcessPendingAsync(cancellationToken);
            _logger.LogInformation("Startup EDGAR import completed: {Processed} processed, {Failed} failed.", result.ProcessedCount, result.FailedCount);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "EDGAR import at startup failed.");
        }
    }

    public Task StopAsync(CancellationToken cancellationToken) => Task.CompletedTask;
}
