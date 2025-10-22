using Global.Objects.Errors;
using Global.Objects.Functional;
using Global.Objects.Results;
using Infrastructure.Core.Models.Security;

namespace Infrastructure.Core.Interfaces.Security;

public interface IKeyRepository
{
    Task<Result<Unit, GenericError>> AddAsync(KeyPair keyPair);
    public Task<Result<Unit, GenericError>> DeactivateKeyAsync(Guid keyId);
    public Task<Result<KeyPair, GenericError>> GetByIdAsync(Guid keyId);
}