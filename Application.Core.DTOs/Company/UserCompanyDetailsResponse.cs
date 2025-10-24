namespace Application.Core.DTOs.Company;

public sealed record UserCompanyDetailsResponse
{
    public required Guid CompanyId { get; init; }
    public required string Ruc { get; init; }
    public required string RazonSocial { get; init; }
    public required string DomicilioLegal { get; init; }
    public required string? Telefono { get; init; }
    public required string Email { get; init; }
    public DateTime? FechaConstitucion { get; set; }
    public required bool IsMype { get; init; }
    public required LegalRepresentativeDto? LegalRepresentative { get; init; }
    public required BankAccountDto? BankAccount { get; init; }
}