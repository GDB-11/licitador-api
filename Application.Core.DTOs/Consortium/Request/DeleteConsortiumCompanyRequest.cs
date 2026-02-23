using System.Text.Json.Serialization;

namespace Application.Core.DTOs.Consortium.Request;

public sealed record DeleteConsortiumCompanyRequest
{
    [JsonPropertyName("consortiumCompanyId")]
    public required Guid ConsortiumCompanyId { get; init; }
    [JsonPropertyName("companyId")]
    public required Guid CompanyId { get; init; }
}