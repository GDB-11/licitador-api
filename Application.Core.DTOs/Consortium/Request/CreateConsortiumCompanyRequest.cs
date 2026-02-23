using System.Text.Json.Serialization;
using Application.Core.DTOs.Consortium.DTO;

namespace Application.Core.DTOs.Consortium.Request;

public sealed record CreateConsortiumCompanyRequest
{
    [JsonPropertyName("companyId")]
    public required Guid CompanyId { get; init; }
    [JsonPropertyName("ruc")]
    public required string Ruc { get; init; }
    [JsonPropertyName("rnpRegistration")]
    public required string RnpRegistration { get; init; }
    [JsonPropertyName("razonSocial")]
    public required string RazonSocial { get; init; }
    [JsonPropertyName("nombreComercial")]
    public required string NombreComercial { get; init; }
    [JsonPropertyName("rnpValidUntil")]
    public required DateOnly RnpValidUntil { get; init; }
    [JsonPropertyName("mainActivity")]
    public string? MainActivity { get; init; }
    [JsonPropertyName("domicilioFiscal")]
    public required string DomicilioFiscal { get; init; }
    [JsonPropertyName("contactPhone")]
    public required string ContactPhone { get; init; }
    [JsonPropertyName("contactEmail")]
    public required string ContactEmail { get; init; }
    [JsonPropertyName("dni")]
    public required string Dni { get; init; }
    [JsonPropertyName("fullName")]
    public required string FullName { get; init; }
    [JsonPropertyName("position")]
    public required string Position { get; init; }
}