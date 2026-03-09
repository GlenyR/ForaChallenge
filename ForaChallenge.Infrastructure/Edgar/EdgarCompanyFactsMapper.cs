using System.Text.Json;
using ForaChallenge.Application.Services;
using ForaChallenge.Domain.Entities;
using ForaChallenge.Domain.ValueObjects;
using Microsoft.Extensions.Logging;

namespace ForaChallenge.Infrastructure.Edgar;

public class EdgarCompanyFactsMapper : IEdgarCompanyFactsMapper
{
    private readonly ILogger<EdgarCompanyFactsMapper> _logger;

    public EdgarCompanyFactsMapper(ILogger<EdgarCompanyFactsMapper> logger) => _logger = logger;

    private const string TaxonomyUsGaap = "us-gaap";             // US GAAP
    private const string ConceptNetIncomeLoss = "NetIncomeLoss"; // Net income (NetIncomeLoss, 10-K)
    private const string UnitsUsd = "USD";                        // Values in dollars (NetIncomeLoss in USD)
    private const string Form10K = "10-K";                        // Filter 10-K only (exclude 10-Q, 8-K, etc.)
    private static readonly (int Min, int Max) YearRange = (2018, 2022); // Calendar Year: frames CY2018–CY2022

    public EdgarMapResult Map(string companyFactsJson, Cik cik)
    {
        var company = new Company { Cik = cik, Name = string.Empty };
        var annualIncomes = new List<CompanyAnnualIncome>();

        try
        {
            using var doc = JsonDocument.Parse(companyFactsJson);
            var root = doc.RootElement;

            if (root.TryGetProperty("entityName", out var entityNameEl))
                company.Name = entityNameEl.GetString() ?? string.Empty;

            if (!root.TryGetProperty("facts", out var facts) ||
                !facts.TryGetProperty(TaxonomyUsGaap, out var usGaap) ||
                !usGaap.TryGetProperty(ConceptNetIncomeLoss, out var netIncome) ||
                !netIncome.TryGetProperty("units", out var units) ||
                !units.TryGetProperty(UnitsUsd, out var usdArray) ||
                usdArray.ValueKind != JsonValueKind.Array)
                return new EdgarMapResult(company, annualIncomes);

            var candidates = new List<(int Year, DateTime? Filed, decimal Value)>();

            foreach (var item in usdArray.EnumerateArray())
            {

                if (item.TryGetProperty("form", out var formEl) && formEl.GetString() != Form10K)
                    continue;
                if (!item.TryGetProperty("fy", out var fyEl) || !fyEl.TryGetInt32(out var fy))
                    continue;

                if (fy < YearRange.Min || fy > YearRange.Max)
                    continue;

                if (!item.TryGetProperty("val", out var valEl) || !valEl.TryGetDecimal(out var val))
                    continue;

                DateTime? filed = null;
                if (item.TryGetProperty("filed", out var filedEl) && filedEl.ValueKind == JsonValueKind.String)
                {
                    if (DateTime.TryParse(filedEl.GetString(), out var d))
                        filed = d;
                }

                candidates.Add((fy, filed, val));
            }


            foreach (var g in candidates.GroupBy(x => x.Year))
            {
                var best = g.OrderByDescending(x => x.Filed ?? DateTime.MinValue).First();
                annualIncomes.Add(new CompanyAnnualIncome
                {
                    Year = best.Year,
                    Value = Money.From(best.Value) 
                });
            }

            annualIncomes.Sort((a, b) => a.Year.CompareTo(b.Year));
        }
        catch (JsonException ex)
        {
            _logger.LogWarning(ex, "Invalid or unexpected Company Facts JSON for CIK {Cik}; returning empty result.", cik.Value);
        }

        return new EdgarMapResult(company, annualIncomes);
    }
}
