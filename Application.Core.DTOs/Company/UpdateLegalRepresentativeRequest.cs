using System.ComponentModel.DataAnnotations;

namespace Application.Core.DTOs.Company;

public sealed record UpdateLegalRepresentativeRequest
{
    [Required]
    [StringLength(255, MinimumLength = 1)]
    public required string FullName { get; init; }

    [Required]
    [StringLength(50)]
    public required string DocumentType { get; init; }

    [Required]
    [StringLength(50, MinimumLength = 1)]
    public required string DocumentNumber { get; init; }

    public string? NationalIdImage { get; init; } // Base64
}