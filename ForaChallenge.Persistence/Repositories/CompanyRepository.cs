using ForaChallenge.Application.Repositories;
using ForaChallenge.Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace ForaChallenge.Persistence.Repositories;

public class CompanyRepository : ICompanyRepository
{
    private readonly ForaDbContext _db;

    public CompanyRepository(ForaDbContext db) => _db = db;

    public async Task<IReadOnlyList<Company>> GetAllAsync(bool includeAnnualIncomes = false, CancellationToken cancellationToken = default)
    {
        var query = _db.Companies.AsNoTracking();
        if (includeAnnualIncomes)
            query = query.Include(c => c.AnnualIncomes);
        return await query.ToListAsync(cancellationToken);
    }

    public async Task<Company?> GetByIdAsync(int id, bool includeAnnualIncomes = false, CancellationToken cancellationToken = default)
    {
        var query = _db.Companies.AsNoTracking();
        if (includeAnnualIncomes)
            query = query.Include(c => c.AnnualIncomes);
        return await query.FirstOrDefaultAsync(c => c.Id == id, cancellationToken);
    }

    public async Task<Company?> GetByCikAsync(string cik, bool includeAnnualIncomes = false, CancellationToken cancellationToken = default)
    {
        var normalized = Domain.ValueObjects.Cik.From(cik);
        var query = _db.Companies.AsNoTracking().Where(c => c.Cik == normalized);
        if (includeAnnualIncomes)
            query = query.Include(c => c.AnnualIncomes);
        return await query.FirstOrDefaultAsync(cancellationToken);
    }

    public async Task AddAsync(Company company, IReadOnlyList<CompanyAnnualIncome>? annualIncomes = null, CancellationToken cancellationToken = default)
    {
        if (annualIncomes != null && annualIncomes.Count > 0)
        {
            foreach (var income in annualIncomes)
                company.AnnualIncomes.Add(income);
        }
        _db.Companies.Add(company);
        await _db.SaveChangesAsync(cancellationToken);
    }

    public async Task AddOrUpdateByCikAsync(Company company, IReadOnlyList<CompanyAnnualIncome>? annualIncomes, CancellationToken cancellationToken = default)
    {
        var cik = company.Cik;
        // Compare using value object so the value converter is used for the parameter (Cik -> string).
        var existing = await _db.Companies.Include(c => c.AnnualIncomes).FirstOrDefaultAsync(c => c.Cik == cik, cancellationToken);

        if (existing != null)
        {
            existing.Name = company.Name;
            existing.AnnualIncomes.Clear();
            if (annualIncomes != null)
            {
                foreach (var inc in annualIncomes)
                    existing.AnnualIncomes.Add(new CompanyAnnualIncome { Year = inc.Year, Value = inc.Value });
            }
        }
        else
        {
            if (annualIncomes != null && annualIncomes.Count > 0)
            {
                foreach (var income in annualIncomes)
                    company.AnnualIncomes.Add(income);
            }
            _db.Companies.Add(company);
        }

        await _db.SaveChangesAsync(cancellationToken);
    }
}
