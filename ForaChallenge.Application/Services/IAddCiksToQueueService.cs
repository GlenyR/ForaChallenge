namespace ForaChallenge.Application.Services;

public interface IAddCiksToQueueService
{
    Task<int> AddAsync(IReadOnlyList<int> cikNumbers, CancellationToken cancellationToken = default);
}
