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
    public required bool IsOwnCompanyLeader { get; init; }

    [StringLength(50)]
    public string? LeaderConsortiumCompanyId { get; init; }

    [Required]
    [MinLength(2)]
    [MaxLength(3)]
    public required List<ConsortiumMember> Members { get; init; }

    [Required]
    public required bool AutorizaNotificacionesEmail { get; init; }

    [EmailAddress]
    [StringLength(255)]
    public string? EmailNotificaciones { get; init; }

    [StringLength(20)]
    public string? NumeroFicha { get; init; }

    [StringLength(20)]
    public string? NumeroAsiento { get; init; }
}

public sealed record ConsortiumMember
{
    [StringLength(50)]
    public string? ConsortiumCompanyId { get; init; }

    [Required]
    public required bool EsEmpresaPropia { get; init; }

    [Required]
    [Range(0.01, 100)]
    public required double PorcentajeParticipacion { get; init; }
}