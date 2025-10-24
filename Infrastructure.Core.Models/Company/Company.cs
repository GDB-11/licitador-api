namespace Infrastructure.Core.Models.Company;

public sealed class Company
{
    public Guid CompanyId { get; init; }
    public required string Ruc { get; init; }
    public required string RazonSocial { get; init; }
    public required string DomicilioLegal { get; init; }
    public string? Telefono { get; init; }
    public required string Email { get; init; }
    public DateTime? FechaConstitucion { get; set; }
    public bool IsMype { get; init; }
    public DateTime CreatedDate { get; init; }
    public DateTime? UpdatedDate { get; init; }
}