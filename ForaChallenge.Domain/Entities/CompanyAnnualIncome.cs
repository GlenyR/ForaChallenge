using ForaChallenge.Domain.ValueObjects;

namespace ForaChallenge.Domain.Entities;

public class CompanyAnnualIncome
{
    public int Id { get; set; }
    public int CompanyId { get; set; }
    public int Year { get; set; }
    public Money Value { get; set; }
    public Company Company { get; set; } = null!;
}
