using ForaChallenge.Domain.Enums;
using ForaChallenge.Domain.ValueObjects;

namespace ForaChallenge.Domain.Entities;

public class CikImport
{
    public int Id { get; set; }
    public Cik Cik { get; set; }
    public DateTime? CreatedAt { get; set; }
    public DateTime? ProcessedAt { get; set; }
    public CikImportStatus Status { get; set; } = CikImportStatus.Pending;
    public string? Message { get; set; }
}

