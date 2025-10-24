namespace Infrastructure.Core.Models.Company;

public sealed class BankAccount
{
    public Guid BankAccountId { get; init; }
    public Guid CompanyId { get; init; }
    public required string BankName { get; init; }
    public required string AccountNumber { get; init; }
    public required string CciCode { get; init; }
    public bool IsActive { get; init; }
    public DateTime CreatedDate { get; init; }
    public DateTime? UpdatedDate { get; init; }
}