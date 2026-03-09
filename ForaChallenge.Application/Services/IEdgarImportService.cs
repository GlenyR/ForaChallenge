namespace ForaChallenge.Application.Services;

public interface IEdgarImportService
{
    Task<EdgarImportResult> ProcessPendingAsync(CancellationToken cancellationToken = default);
}

public sealed record EdgarImportResult(int ProcessedCount, int FailedCount);
