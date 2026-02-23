using System.Text.Json.Serialization;

namespace Application.Core.DTOs.Consortium.DTO;

public sealed record ConsortiumCompanyLegalRepresentativeData
{
    [JsonPropertyName("consortiumLegalRepresentativeId")]
    public Guid? ConsortiumLegalRepresentativeId { get; init; }
    [JsonPropertyName("consortiumCompanyId")]
    public Guid? ConsortiumCompanyId { get; init; }
    [JsonPropertyName("dni")]
    public string? Dni { get; init; }
    [JsonPropertyName("fullName")]
    public string? FullName { get; init; }
    [JsonPropertyName("position")]
    public string? Position { get; init; }
    [JsonPropertyName("isActive")]
    public bool? IsActive { get; init; }
}