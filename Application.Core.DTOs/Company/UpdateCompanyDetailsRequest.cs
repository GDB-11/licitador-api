using System.ComponentModel.DataAnnotations;

namespace Application.Core.DTOs.Company;

public sealed record UpdateCompanyDetailsRequest
{
    [Required]
    public required Guid CompanyId { get; init; }

    [Required]
    [StringLength(11, MinimumLength = 11)]
    [RegularExpression(@"^\d{11}$", ErrorMessage = "RUC must be exactly 11 digits")]
    public required string Ruc { get; init; }

    [Required]
    [StringLength(500, MinimumLength = 1)]
    public required string RazonSocial { get; init; }

    [Required]
    [StringLength(1000, MinimumLength = 1)]
    public required string DomicilioLegal { get; init; }

    [StringLength(50)]
    public string? Telefono { get; init; }

    [Required]
    [EmailAddress]
    [StringLength(255)]
    public required string Email { get; init; }

    public DateTime? FechaConstitucion { get; init; }

    [Required]
    public required bool IsMype { get; init; }

    public UpdateLegalRepresentativeRequest? LegalRepresentative { get; init; }

    public UpdateBankAccountRequest? BankAccount { get; init; }
}