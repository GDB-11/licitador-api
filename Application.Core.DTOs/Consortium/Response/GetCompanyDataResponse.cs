using System.Text.Json.Serialization;
using Application.Core.DTOs.Consortium.DTO;

namespace Application.Core.DTOs.Consortium.Response;

public sealed record GetCompanyDataResponse
{
    [JsonPropertyName("company")]
    public required ConsortiumCompanyData Company { get; init; }
}