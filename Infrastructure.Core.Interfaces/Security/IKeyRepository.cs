using BindSharp;
using Infrastructure.Core.DTOs.Security;
using Infrastructure.Core.Models.Security;

namespace Infrastructure.Core.Interfaces.Security;

public interface IKeyRepository
{
    Task<Result<Unit, SecurityError>> AddAsync(KeyPair keyPair);
    Task<Result<Unit, SecurityError>> DeactivateKeyAsync(Guid keyId);
    Task<Result<KeyPair?, SecurityError>> GetByIdAsync(Guid keyId);
}