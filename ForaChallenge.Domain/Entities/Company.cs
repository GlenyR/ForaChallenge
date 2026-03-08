using ForaChallenge.Domain.ValueObjects;

namespace ForaChallenge.Domain.Entities;

public class Company
{
    public int Id { get; set; }
    public Cik Cik { get; set; } 
    public string Name { get; set; } = string.Empty;
    public ICollection<CompanyAnnualIncome> AnnualIncomes { get; set; } = [];

}

