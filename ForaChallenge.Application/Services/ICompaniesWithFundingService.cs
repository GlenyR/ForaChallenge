namespace ForaChallenge.Application.Services;

public interface ICompaniesWithFundingService
{
    Task<IReadOnlyList<CompanyFundingDto>> GetAsync(char? nameStartsWith, CancellationToken cancellationToken = default);
}

public sealed record CompanyFundingDto(int Id, string Name, decimal StandardFundableAmount, decimal SpecialFundableAmount);
