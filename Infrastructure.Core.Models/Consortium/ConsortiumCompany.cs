namespace Infrastructure.Core.Models.Consortium;

public sealed record ConsortiumCompany
{
    public required Guid ConsortiumCompanyId { get; init; }
    public required Guid CompanyId { get; init; }
    public required string Ruc { get; init; }
    public required string RnpRegistration { get; init; }
    public required string RazonSocial { get; init; }
    public required string NombreComercial { get; init; }
    public required DateOnly RnpValidUntil { get; init; }
    public string? MainActivity { get; init; }
    public required string DomicilioFiscal { get; init; }
    public required string ContactPhone { get; init; }
    public required string ContactEmail { get; init; }
    public bool IsActive { get; init; }
    public DateTime CreatedDate { get; init; }
    public DateTime UpdatedDate { get; init; }

    public ConsortiumCompanyLegalRepresentative? LegalRepresentative { get; init; }
}