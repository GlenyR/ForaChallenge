using ForaChallenge.Domain.Entities;
using System;
using System.Collections.Generic;
using System.Text;

namespace ForaChallenge.Application.Repositories;

public interface ICikImportRepository
{
    Task<IReadOnlyList<CikImport>> GetPendingAsync(CancellationToken cancellationToken = default);
    Task MarkAsProcessedAsync(int id, DateTime processedAt, CancellationToken cancellationToken = default);

    Task AddRangeAsync(IEnumerable<CikImport> cikImports, CancellationToken cancellationToken = default);

    Task<int> CountAsync(CancellationToken cancellationToken = default);
    Task<IReadOnlyList<string>> GetExistingCikValuesAsync(CancellationToken cancellationToken = default);
}

