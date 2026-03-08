
using ForaChallenge.Domain.ValueObjects;

namespace ForaChallenge.Application.Services;

public interface ISecEdgarApiClient
{
    Task<string> GetCompanyFactsAsync(Cik cik, CancellationToken cancellationToken = default);
}

