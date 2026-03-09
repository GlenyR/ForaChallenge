using System.Net;
using ForaChallenge.Application.Repositories;
using ForaChallenge.Application.Services;
using ForaChallenge.Domain.ValueObjects;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace ForaChallenge.Infrastructure.Edgar;

/// <summary>To count separately from retryable failures.</summary>
internal sealed class CikMarkedAsExceptionException : Exception { }

public class EdgarImportService(
    IServiceScopeFactory scopeFactory,
    ICikImportRepository cikImportRepository,
    ILogger<EdgarImportService> logger,
    IConfiguration configuration) : IEdgarImportService
{
    private const int MinConcurrentRequests = 1;

    private readonly IServiceScopeFactory _scopeFactory = scopeFactory;
    private readonly ICikImportRepository _cikImportRepository = cikImportRepository;
    private readonly ILogger<EdgarImportService> _logger = logger;
    private readonly IConfiguration _configuration = configuration;

    public async Task<EdgarImportResult> ProcessPendingAsync(CancellationToken cancellationToken = default)
    {
        var pending = await _cikImportRepository.GetPendingAsync(cancellationToken);
        if (pending.Count == 0)
        {
            _logger.LogInformation("No pending CIKs to process.");
            return new EdgarImportResult(0, 0);
        }

        var maxConcurrent = _configuration.GetValue("SecEdgar:MaxConcurrentRequests", MinConcurrentRequests);
        var rateLimit = _configuration.GetValue("SecEdgar:RateLimitMaxConcurrent", 10);
        if (maxConcurrent < MinConcurrentRequests) maxConcurrent = MinConcurrentRequests;
        if (rateLimit < MinConcurrentRequests) rateLimit = MinConcurrentRequests;
        if (maxConcurrent > rateLimit) maxConcurrent = rateLimit;

        _logger.LogInformation("Processing {Count} pending CIKs with max concurrency {MaxConcurrent}.", pending.Count, maxConcurrent);

        var semaphore = new SemaphoreSlim(maxConcurrent);
        var processed = 0;
        var failed = 0;
        var exceptionCount = 0;
        var lockObj = new object();

        var tasks = pending.Select(async cikImport =>
        {
            await semaphore.WaitAsync(cancellationToken);
            try
            {
                await ProcessOneInNewScopeAsync(cikImport.Id, cikImport.Cik, cancellationToken);
                lock (lockObj) { processed++; }
            }
            catch (CikMarkedAsExceptionException)
            {
                lock (lockObj) { exceptionCount++; }
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Failed to import CIK {Cik} (Id {Id}); leaving Pending for retry.", cikImport.Cik.Value, cikImport.Id);
                lock (lockObj) { failed++; }
            }
            finally
            {
                semaphore.Release();
            }
        });

        await Task.WhenAll(tasks);

        _logger.LogInformation("EDGAR import finished: {Processed} processed, {Failed} failed, {Exception} exception.", processed, failed, exceptionCount);
        return new EdgarImportResult(processed, failed, exceptionCount);
    }

    private async Task ProcessOneInNewScopeAsync(int cikImportId, Cik cik, CancellationToken cancellationToken)
    {
        using var scope = _scopeFactory.CreateScope();
        var apiClient = scope.ServiceProvider.GetRequiredService<ISecEdgarApiClient>();
        var mapper = scope.ServiceProvider.GetRequiredService<IEdgarCompanyFactsMapper>();
        var companyRepo = scope.ServiceProvider.GetRequiredService<ICompanyRepository>();
        var cikRepo = scope.ServiceProvider.GetRequiredService<ICikImportRepository>();

        string json;
        try
        {
            json = await apiClient.GetCompanyFactsAsync(cik, cancellationToken);
        }
        catch (HttpRequestException ex) when (ex.StatusCode == HttpStatusCode.NotFound)
        {
            await cikRepo.MarkAsExceptionAsync(cikImportId, "The specified key does not exist.", cancellationToken);
            throw new CikMarkedAsExceptionException();
        }

        var result = mapper.Map(json, cik);

        if (string.IsNullOrWhiteSpace(result.Company.Name))
        {
            await cikRepo.MarkAsExceptionAsync(cikImportId, "Empty File", cancellationToken);
            throw new CikMarkedAsExceptionException();
        }

        await companyRepo.AddOrUpdateByCikAsync(result.Company, result.AnnualIncomes.Count > 0 ? result.AnnualIncomes : null, cancellationToken);
        await cikRepo.MarkAsProcessedAsync(cikImportId, DateTime.UtcNow, cancellationToken);
    }
}
