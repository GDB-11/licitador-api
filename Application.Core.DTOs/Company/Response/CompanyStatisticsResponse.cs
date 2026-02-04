namespace Application.Core.DTOs.Company.Response;

public sealed record CompanyStatisticsResponse
{
    public int GeneratedDocuments { get; init; }
    public byte CompleteProfilePercentage { get; init; }
    public int ConsortiaCompanies { get; init; }
}