namespace Application.Core.DTOs.Consortium.Request;

public sealed record GetAllCompaniesRequest
{
    public required Guid CompanyId { get; init; }
}