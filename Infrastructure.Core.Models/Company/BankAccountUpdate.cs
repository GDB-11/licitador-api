namespace Infrastructure.Core.Models.Company;

public sealed class BankAccountUpdate
{
    public required string BankName { get; init; }
    public required string AccountNumber { get; init; }
    public required string CciCode { get; init; }
}