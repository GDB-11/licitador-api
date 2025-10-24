namespace Infrastructure.Core.Models.Company;

public sealed class LegalRepresentative
{
    public Guid LegalRepresentativeId { get; init; }
    public Guid CompanyId { get; init; }
    public required string FullName { get; init; }
    public required string DocumentType { get; init; }
    public required string DocumentNumber { get; init; }
    public string? PowerRegistrationLocation { get; init; }
    public string? PowerRegistrationSheet { get; init; }
    public string? PowerRegistrationEntry { get; init; }
    public byte[]? NationalIdImage { get; init; }
    public bool IsActive { get; init; }
    public DateTime CreatedDate { get; init; }
    public DateTime? UpdatedDate { get; init; }
}