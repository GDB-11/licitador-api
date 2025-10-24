namespace Infrastructure.Core.Models.Company;

public sealed class CompanyDetails
{
    public required Company Company { get; init; }
    public LegalRepresentative? LegalRepresentative { get; init; }
    public BankAccount? BankAccount { get; init; }
}