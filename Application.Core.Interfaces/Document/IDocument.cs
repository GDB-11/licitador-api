using Application.Core.DTOs.Document;
using Global.Objects.Document;
using Global.Objects.Results;

namespace Application.Core.Interfaces.Document;

public interface IDocument
{
    Task<Result<byte[], DocumentError>> GenerateAnnexesAsync(Guid userId, GenerateAnnexesRequest request);
}