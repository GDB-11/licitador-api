using System.Text.Json.Serialization;
using Infrastructure.Core.Models.Consortium;

namespace Application.Core.DTOs.Consortium.Response;

public sealed record GetAllCompaniesResponse
{
    [JsonPropertyName("companies")]
    public required List<ConsortiumCompany> Companies { get; init; } = [];
}