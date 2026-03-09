using ForaChallenge.Application.Repositories;
using ForaChallenge.Domain.Entities;
using ForaChallenge.Domain.Enums;
using Microsoft.EntityFrameworkCore;

namespace ForaChallenge.Persistence.Repositories;

public class CikImportRepository : ICikImportRepository
{
    private readonly ForaDbContext _db;

    public CikImportRepository(ForaDbContext db) => _db = db;

    public async Task<IReadOnlyList<CikImport>> GetPendingAsync(CancellationToken cancellationToken = default)
    {
        return await _db.CikImports
            .AsNoTracking()
            .Where(x => x.Status == CikImportStatus.Pending)
            .OrderBy(x => x.Id)
            .ToListAsync(cancellationToken);
    }

    public async Task MarkAsProcessedAsync(int id, DateTime processedAt, CancellationToken cancellationToken = default)
    {
        var entity = await _db.CikImports.FindAsync([id], cancellationToken);
        if (entity == null) return;
        entity.Status = CikImportStatus.Processed;
        entity.ProcessedAt = processedAt;
        entity.Message = null;
        await _db.SaveChangesAsync(cancellationToken);
    }

    public async Task MarkAsExceptionAsync(int id, string message, CancellationToken cancellationToken = default)
    {
        var entity = await _db.CikImports.FindAsync([id], cancellationToken);
        if (entity == null) return;
        entity.Status = CikImportStatus.Exception;
        entity.Message = message;
        entity.ProcessedAt = null;
        await _db.SaveChangesAsync(cancellationToken);
    }

    public async Task AddRangeAsync(IEnumerable<CikImport> cikImports, CancellationToken cancellationToken = default)
    {
        await _db.CikImports.AddRangeAsync(cikImports, cancellationToken);
        await _db.SaveChangesAsync(cancellationToken);
    }

    public async Task<int> CountAsync(CancellationToken cancellationToken = default)
    {
        return await _db.CikImports.CountAsync(cancellationToken);
    }

    public async Task ResetAllToPendingAsync(CancellationToken cancellationToken = default)
    {
        await _db.CikImports.ExecuteUpdateAsync(
            s => s.SetProperty(x => x.Status, CikImportStatus.Pending)
                .SetProperty(x => x.ProcessedAt, (DateTime?)null)
                .SetProperty(x => x.Message, (string?)null),
            cancellationToken);
    }

    public async Task<IReadOnlyList<string>> GetExistingCikValuesAsync(CancellationToken cancellationToken = default)
    {
        return await _db.CikImports.AsNoTracking().Select(x => x.Cik.Value).ToListAsync(cancellationToken);
    }
}
