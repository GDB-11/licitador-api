using System.ComponentModel.DataAnnotations;

namespace Application.Core.DTOs.Document.Request;

public sealed record GenerateAnnexesConsortiumRequest
{
    [Required]
    [StringLength(100, MinimumLength = 1)]
    public required string LicitacionNumber { get; init; }

    [Required]
    [StringLength(255, MinimumLength = 1)]
    public required string EntityName { get; init; }

    [Required]
    [StringLength(500, MinimumLength = 1)]
    public required string PurchaseObject { get; init; }

    [Required]
    [StringLength(100, MinimumLength = 1)]
    public required string City { get; init; }
    
    [Required]
    [StringLength(100, MinimumLength = 1)]
    public required string ConsortiumName { get; init; }

    [Required]
    public required bool AutorizaNotificacionesEmail { get; init; }

    [EmailAddress]
    [StringLength(255)]
    public string? EmailNotificaciones { get; init; }
}