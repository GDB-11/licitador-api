namespace Infrastructure.Core.Models.Company;

public sealed class UserCompany
{
    public Guid UserCompanyId { get; init; }
    public Guid UserId { get; init; }
    public Guid CompanyId { get; init; }
    public string? Role { get; init; }
    public bool IsActive { get; init; }
    public DateTime CreatedDate { get; init; }
    public DateTime? UpdatedDate { get; init; }
}