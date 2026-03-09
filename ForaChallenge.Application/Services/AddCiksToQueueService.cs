using ForaChallenge.Application.Repositories;
using ForaChallenge.Domain.Entities;
using ForaChallenge.Domain.Enums;
using ForaChallenge.Domain.ValueObjects;

namespace ForaChallenge.Application.Services;

public class AddCiksToQueueService : IAddCiksToQueueService
{
    private readonly ICikImportRepository _cikImportRepository;

    public AddCiksToQueueService(ICikImportRepository cikImportRepository)
    {
        _cikImportRepository = cikImportRepository;
    }

    public async Task<int> AddAsync(IReadOnlyList<int> cikNumbers, CancellationToken cancellationToken = default)
    {
        if (cikNumbers == null || cikNumbers.Count == 0)
            return 0;

        var existing = await _cikImportRepository.GetExistingCikValuesAsync(cancellationToken);
        var existingSet = existing.ToHashSet(StringComparer.OrdinalIgnoreCase);

        var toAdd = new List<CikImport>();
        var now = DateTime.UtcNow;

        foreach (var num in cikNumbers.Distinct())
        {
            var cik = Cik.From(num);
            if (existingSet.Contains(cik.Value))
                continue;
            existingSet.Add(cik.Value);
            toAdd.Add(new CikImport { Cik = cik, Status = CikImportStatus.Pending, CreatedAt = now });
        }

        if (toAdd.Count == 0)
            return 0;

        await _cikImportRepository.AddRangeAsync(toAdd, cancellationToken);
        return toAdd.Count;
    }
}
