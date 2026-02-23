namespace Infrastructure.Core.Models.Consortium;

public sealed record ConsortiumCompanyLegalRepresentative
{
    public required Guid ConsortiumLegalRepresentativeId { get; init; }
    public required Guid ConsortiumCompanyId { get; init; }
    public required string Dni { get; init; }
    public required string FullName { get; init; }
    public required string Position { get; init; }
    public bool IsActive { get; init; }
    public DateTime CreatedDate { get; init; }
    public DateTime UpdatedDate { get; init; }
}