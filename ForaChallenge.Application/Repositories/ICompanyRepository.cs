using ForaChallenge.Domain.Entities;

namespace ForaChallenge.Application.Repositories;

public interface ICompanyRepository
{
    Task<IReadOnlyList<Company>> GetAllAsync(bool includeAnnualIncomes = false, CancellationToken cancellationToken = default);
    Task<Company?> GetByIdAsync(int id, bool includeAnnualIncomes = false, CancellationToken cancellationToken = default);
    Task<Company?> GetByCikAsync(string cik, bool includeAnnualIncomes = false, CancellationToken cancellationToken = default);
    Task AddAsync(Company company, IReadOnlyList<CompanyAnnualIncome>? annualIncomes = null, CancellationToken cancellationToken = default);
    Task AddOrUpdateByCikAsync(Company company, IReadOnlyList<CompanyAnnualIncome>? annualIncomes, CancellationToken cancellationToken = default);
}
