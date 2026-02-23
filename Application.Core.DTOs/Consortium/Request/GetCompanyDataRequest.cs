namespace Application.Core.DTOs.Consortium.Request;

public sealed record GetCompanyDataRequest
{
    public required Guid CompanyId { get; init; }
    public required Guid ConsortiumCompanyId { get; init; }
}