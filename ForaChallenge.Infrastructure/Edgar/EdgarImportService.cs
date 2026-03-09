using ForaChallenge.Application.Repositories;
using ForaChallenge.Application.Services;
using ForaChallenge.Domain.Entities;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;

namespace ForaChallenge.Infrastructure.Edgar;

public class EdgarImportService : IEdgarImportService
{
    private const int MinConcurrentRequests = 1;

    private readonly ICikImportRepository _cikImportRepository;
    private readonly ICompanyRepository _companyRepository;
    private readonly ISecEdgarApiClient _secEdgarApiClient;
    private readonly IEdgarCompanyFactsMapper _mapper;
    private readonly ILogger<EdgarImportService> _logger;
    private readonly IConfiguration _configuration;

    public EdgarImportService(
        ICikImportRepository cikImportRepository,
        ICompanyRepository companyRepository,
        ISecEdgarApiClient secEdgarApiClient,
        IEdgarCompanyFactsMapper mapper,
        ILogger<EdgarImportService> logger,
        IConfiguration configuration)
    {
        _cikImportRepository = cikImportRepository;
        _companyRepository = companyRepository;
        _secEdgarApiClient = secEdgarApiClient;
        _mapper = mapper;
        _logger = logger;
        _configuration = configuration;
    }

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
        var lockObj = new object();

        var tasks = pending.Select(async cikImport =>
        {
            await semaphore.WaitAsync(cancellationToken);
            try
            {
                await ProcessOneAsync(cikImport, cancellationToken);
                lock (lockObj) { processed++; }
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

        _logger.LogInformation("EDGAR import finished: {Processed} processed, {Failed} failed.", processed, failed);
        return new EdgarImportResult(processed, failed);
    }

    private async Task ProcessOneAsync(CikImport cikImport, CancellationToken cancellationToken)
    {
        var json = await _secEdgarApiClient.GetCompanyFactsAsync(cikImport.Cik, cancellationToken);
        var result = _mapper.Map(json, cikImport.Cik);

        await _companyRepository.AddAsync(result.Company, result.AnnualIncomes.Count > 0 ? result.AnnualIncomes : null, cancellationToken);
        await _cikImportRepository.MarkAsProcessedAsync(cikImport.Id, DateTime.UtcNow, cancellationToken);
    }
}
