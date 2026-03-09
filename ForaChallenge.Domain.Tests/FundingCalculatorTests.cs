using ForaChallenge.Domain.Entities;
using ForaChallenge.Domain.Services;
using ForaChallenge.Domain.ValueObjects;

namespace ForaChallenge.Domain.Tests;

public class FundingCalculatorTests
{
    private static Company CompanyWithIncomes(string name, params (int year, decimal value)[] incomes)
    {
        var company = new Company { Id = 1, Cik = Cik.From(1), Name = name };
        foreach (var (year, value) in incomes)
            company.AnnualIncomes.Add(new CompanyAnnualIncome { CompanyId = company.Id, Year = year, Value = Money.From(value) });
        return company;
    }

    private static Company CompanyWithAllYears(string name, decimal income2018, decimal income2019, decimal income2020, decimal income2021, decimal income2022)
        => CompanyWithIncomes(name, (2018, income2018), (2019, income2019), (2020, income2020), (2021, income2021), (2022, income2022));

    [Fact]
    public void CalculateStandardFundableAmount_WhenNoIncomes_ReturnsZero()
    {
        var company = new Company { Id = 1, Cik = Cik.From(1), Name = "Test" };
        Assert.Equal(0, FundingCalculator.CalculateStandardFundableAmount(company));
    }

    [Fact]
    public void CalculateStandardFundableAmount_WhenNullCompany_ReturnsZero()
    {
        Assert.Equal(0, FundingCalculator.CalculateStandardFundableAmount(null!));
    }

    [Fact]
    public void CalculateStandardFundableAmount_WhenMissingAnyRequiredYear_ReturnsZero()
    {
        var company = CompanyWithIncomes("Test", (2018, 100), (2019, 100), (2020, 100), (2021, 100)); // missing 2022
        Assert.Equal(0, FundingCalculator.CalculateStandardFundableAmount(company));
    }

    [Fact]
    public void CalculateStandardFundableAmount_When2021NotPositive_ReturnsZero()
    {
        var company = CompanyWithAllYears("Test", 100, 100, 100, 0, 100);
        Assert.Equal(0, FundingCalculator.CalculateStandardFundableAmount(company));
    }

    [Fact]
    public void CalculateStandardFundableAmount_When2022NotPositive_ReturnsZero()
    {
        var company = CompanyWithAllYears("Test", 100, 100, 100, 100, 0);
        Assert.Equal(0, FundingCalculator.CalculateStandardFundableAmount(company));
    }

    [Fact]
    public void CalculateStandardFundableAmount_WhenIncomeBelow10B_Uses21_51Percent()
    {
        decimal maxIncome = 1_000_000m;
        var company = CompanyWithAllYears("Test", maxIncome, maxIncome, maxIncome, maxIncome, maxIncome);
        decimal expected = Math.Round(maxIncome * 0.2151m, 2);
        Assert.Equal(expected, FundingCalculator.CalculateStandardFundableAmount(company));
    }

    [Fact]
    public void CalculateStandardFundableAmount_WhenIncomeAtOrAbove10B_Uses12_33Percent()
    {
        decimal maxIncome = 10_000_000_000m;
        var company = CompanyWithAllYears("Test", maxIncome, maxIncome, maxIncome, maxIncome, maxIncome);
        decimal expected = Math.Round(maxIncome * 0.1233m, 2);
        Assert.Equal(expected, FundingCalculator.CalculateStandardFundableAmount(company));
    }

    [Fact]
    public void CalculateStandardFundableAmount_UsesHighestIncomeAmongYears()
    {
        var company = CompanyWithAllYears("Test", 100, 200, 300, 400, 500); // max 500
        decimal expected = Math.Round(500m * 0.2151m, 2);
        Assert.Equal(expected, FundingCalculator.CalculateStandardFundableAmount(company));
    }

    [Fact]
    public void CalculateSpecialFundableAmount_WhenStandardIsZero_ReturnsZero()
    {
        var company = CompanyWithIncomes("Apple", (2018, 1), (2019, 1)); // missing years
        Assert.Equal(0, FundingCalculator.CalculateSpecialFundableAmount(company));
    }

    [Fact]
    public void CalculateSpecialFundableAmount_StartsAsStandard()
    {
        var company = CompanyWithAllYears("Beta Corp", 1000, 1000, 1000, 1000, 1000); // no vowel, no decline
        decimal standard = FundingCalculator.CalculateStandardFundableAmount(company);
        Assert.Equal(standard, FundingCalculator.CalculateSpecialFundableAmount(company));
    }

    [Fact]
    public void CalculateSpecialFundableAmount_WhenNameStartsWithVowel_Adds15Percent()
    {
        var company = CompanyWithAllYears("Apple Inc", 1000, 1000, 1000, 1000, 1000);
        decimal standard = FundingCalculator.CalculateStandardFundableAmount(company);
        decimal expected = Math.Round(standard + standard * 0.15m, 2);
        Assert.Equal(expected, FundingCalculator.CalculateSpecialFundableAmount(company));
    }

    [Fact]
    public void CalculateSpecialFundableAmount_When2022LessThan2021_Subtracts25Percent()
    {
        var company = CompanyWithAllYears("Test Co", 1000, 1000, 1000, 1000, 500); // 2022 < 2021
        decimal standard = FundingCalculator.CalculateStandardFundableAmount(company);
        decimal expected = Math.Round(standard - standard * 0.25m, 2);
        Assert.Equal(expected, FundingCalculator.CalculateSpecialFundableAmount(company));
    }

    [Fact]
    public void CalculateSpecialFundableAmount_WhenVowelAndDecline_AppliesBothAdjustments()
    {
        var company = CompanyWithAllYears("Umbrella Inc", 1000, 1000, 1000, 1000, 500);
        decimal standard = FundingCalculator.CalculateStandardFundableAmount(company);
        decimal withVowel = standard + standard * 0.15m;
        decimal withDecline = withVowel - standard * 0.25m;
        decimal expected = Math.Round(Math.Max(0, withDecline), 2);
        Assert.Equal(expected, FundingCalculator.CalculateSpecialFundableAmount(company));
    }

    [Fact]
    public void CalculateSpecialFundableAmount_ResultNeverNegative()
    {
        var company = CompanyWithAllYears("Test Ltd", 100, 100, 100, 100, 1); // big decline
        decimal special = FundingCalculator.CalculateSpecialFundableAmount(company);
        Assert.True(special >= 0);
    }

    [Fact]
    public void Calculate_ReturnsFundableAmountValueObject_WithStandardAndSpecial()
    {
        var company = CompanyWithAllYears("Test Corp", 1000, 1000, 1000, 1000, 1000);
        var result = FundingCalculator.Calculate(company);
        Assert.Equal(FundingCalculator.CalculateStandardFundableAmount(company), result.Standard.Value);
        Assert.Equal(FundingCalculator.CalculateSpecialFundableAmount(company), result.Special.Value);
    }
}
