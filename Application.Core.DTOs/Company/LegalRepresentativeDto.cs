namespace Application.Core.DTOs.Company;

public sealed record LegalRepresentativeDto
{
    public required Guid LegalRepresentativeId { get; init; }
    public required string FullName { get; init; }
    public required string DocumentType { get; init; }
    public required string DocumentNumber { get; init; }
    public required string? NationalIdImage { get; init; } // Base64 encoded
}