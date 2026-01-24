namespace Application.Core.DTOs.Company.Response;

public sealed record UserCompanyResponse
{
    public required Guid CompanyId { get; init; }
    public required string Ruc { get; init; }
    public required string RazonSocial { get; init; }
}