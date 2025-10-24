namespace Infrastructure.Core.Models.Company;

public sealed class LegalRepresentativeUpdate
{
    public required string FullName { get; init; }
    public required string DocumentType { get; init; }
    public required string DocumentNumber { get; init; }
    public byte[]? NationalIdImage { get; init; }
}