namespace Application.Core.DTOs.Company;

public sealed record BankAccountDto
{
    public required Guid BankAccountId { get; init; }
    public required string BankName { get; init; }
    public required string AccountNumber { get; init; }
    public required string CciCode { get; init; }
}