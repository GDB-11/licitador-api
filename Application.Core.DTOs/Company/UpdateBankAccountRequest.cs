using System.ComponentModel.DataAnnotations;

namespace Application.Core.DTOs.Company;

public sealed record UpdateBankAccountRequest
{
    [Required]
    [StringLength(200, MinimumLength = 1)]
    public required string BankName { get; init; }

    [Required]
    [StringLength(50, MinimumLength = 1)]
    public required string AccountNumber { get; init; }

    [Required]
    [StringLength(20, MinimumLength = 20)]
    public required string CciCode { get; init; }
}