using ForaChallenge.Domain.Entities;
using ForaChallenge.Domain.Enums;
using ForaChallenge.Domain.ValueObjects;
using Microsoft.EntityFrameworkCore;

namespace ForaChallenge.Persistence;

public class ForaDbContext : DbContext
{
    public ForaDbContext(DbContextOptions<ForaDbContext> options) : base(options) { }

    public DbSet<Company> Companies => Set<Company>();
    public DbSet<CompanyAnnualIncome> CompanyAnnualIncomes => Set<CompanyAnnualIncome>();
    public DbSet<CikImport> CikImports => Set<CikImport>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<Company>(e =>
        {
            e.HasKey(x => x.Id);
            e.Property(x => x.Name).HasMaxLength(500).IsRequired();
            e.Property(x => x.Cik)
                .HasConversion(c => c.Value, s => Cik.From(s))
                .HasMaxLength(10)
                .IsRequired();
            e.HasIndex(x => x.Cik).IsUnique();
            e.HasMany(x => x.AnnualIncomes)
                .WithOne(x => x.Company)
                .HasForeignKey(x => x.CompanyId)
                .OnDelete(DeleteBehavior.Cascade);
        });

        modelBuilder.Entity<CompanyAnnualIncome>(e =>
        {
            e.HasKey(x => x.Id);
            e.Property(x => x.Year).IsRequired();
            e.Property(x => x.Value)
                .HasConversion(m => m.Value, d => Money.From(d))
                .HasPrecision(18, 2)
                .IsRequired();
            e.HasIndex(x => new { x.CompanyId, x.Year }).IsUnique();
        });

        modelBuilder.Entity<CikImport>(e =>
        {
            e.HasKey(x => x.Id);
            e.Property(x => x.Cik)
                .HasConversion(c => c.Value, s => Cik.From(s))
                .HasMaxLength(10)
                .IsRequired();
            e.HasIndex(x => x.Cik).IsUnique();
            e.Property(x => x.Status)
                .HasConversion(s => (int)s, i => (CikImportStatus)i)
                .IsRequired();
        });
    }
}
