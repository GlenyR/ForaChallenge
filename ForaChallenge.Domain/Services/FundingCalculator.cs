using ForaChallenge.Domain.Entities;
using ForaChallenge.Domain.ValueObjects;

namespace ForaChallenge.Domain.Services;

public static class FundingCalculator
{
    private const decimal IncomeThresholdTenBillion = 10_000_000_000m;
    private const decimal StandardRateHigh = 0.1233m;
    private const decimal StandardRateLow = 0.2151m;
    private const decimal SpecialVowelBonus = 0.15m;
    private const decimal SpecialIncomeDeclinePenalty = 0.25m;

    private static readonly int[] RequiredYears = [ 2018, 2019, 2020, 2021, 2022 ];
    private static readonly int[] YearsRequiringPositiveIncome = [ 2021, 2022 ];
    private static readonly char[] Vowels = ['A', 'E', 'I', 'O', 'U'];


    public static FundableAmount Calculate(Company company)
    {
        decimal standard = CalculateStandard(company);
        decimal special = CalculateSpecial(company, standard);
        return FundableAmount.From(standard, special);
    }

    public static decimal CalculateStandardFundableAmount(Company company) => CalculateStandard(company);

    public static decimal CalculateSpecialFundableAmount(Company company) => CalculateSpecial(company, CalculateStandard(company));

    private static decimal CalculateStandard(Company? company)
    {
        if (company?.AnnualIncomes == null || company.AnnualIncomes.Count == 0)
            return 0;

        var byYear = company.AnnualIncomes.ToDictionary(x => x.Year, x => x.Value.Value);

        foreach (var year in RequiredYears)
        {
            if (!byYear.TryGetValue(year, out _))
                return 0;
        }

        foreach (var year in YearsRequiringPositiveIncome)
        {
            if (byYear[year] <= 0)
                return 0;
        }

        decimal maxIncome = RequiredYears.Max(y => byYear[y]);
        decimal rate = maxIncome >= IncomeThresholdTenBillion ? StandardRateHigh : StandardRateLow;
        return Math.Round(maxIncome * rate, 2);
    }

    private static decimal CalculateSpecial(Company company, decimal standard)
    {
        if (standard == 0)
            return 0;

        decimal amount = standard;

        if (NameStartsWithVowel(company.Name))
            amount += standard * SpecialVowelBonus;

        var byYear = company.AnnualIncomes?.ToDictionary(x => x.Year, x => x.Value.Value) ?? [];
        if (byYear.TryGetValue(2021, out decimal income2021) && byYear.TryGetValue(2022, out decimal income2022) && income2022 < income2021)
            amount -= standard * SpecialIncomeDeclinePenalty;

        return Math.Round(Math.Max(0, amount), 2);
    }

    private static bool NameStartsWithVowel(string name)
    {
        if (string.IsNullOrWhiteSpace(name)) return false;
        char first = char.ToUpperInvariant(name.Trim()[0]);
        return Vowels.Contains(first);
    }
}
