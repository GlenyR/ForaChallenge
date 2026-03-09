using ForaChallenge.Application.Repositories;
using ForaChallenge.Domain.Services;

namespace ForaChallenge.Application.Services;

public class CompaniesWithFundingService : ICompaniesWithFundingService
{
    private readonly ICompanyRepository _companyRepository;

    public CompaniesWithFundingService(ICompanyRepository companyRepository)
    {
        _companyRepository = companyRepository;
    }

    public async Task<IReadOnlyList<CompanyFundingDto>> GetAsync(char? nameStartsWith, CancellationToken cancellationToken = default)
    {
        var companies = await _companyRepository.GetAllAsync(includeAnnualIncomes: true, cancellationToken);
        var list = new List<CompanyFundingDto>();

        foreach (var company in companies)
        {
            if (nameStartsWith.HasValue && (string.IsNullOrWhiteSpace(company.Name) || char.ToUpperInvariant(company.Name.Trim()[0]) != char.ToUpperInvariant(nameStartsWith.Value)))
                continue;

            var funding = FundingCalculator.Calculate(company);
            list.Add(new CompanyFundingDto(
                company.Id,
                company.Name,
                funding.Standard.Value,
                funding.Special.Value));
        }

        return list;
    }
}
