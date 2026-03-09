using ForaChallenge.Domain.Entities;
using ForaChallenge.Domain.ValueObjects;

namespace ForaChallenge.Application.Services;

public interface IEdgarCompanyFactsMapper
{
    EdgarMapResult Map(string companyFactsJson, Cik cik);
}

public sealed record EdgarMapResult(Company Company, IReadOnlyList<CompanyAnnualIncome> AnnualIncomes);
