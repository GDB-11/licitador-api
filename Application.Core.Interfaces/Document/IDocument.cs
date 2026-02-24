using Application.Core.DTOs.Document.Errors;
using Application.Core.DTOs.Document.Request;
using BindSharp;

namespace Application.Core.Interfaces.Document;

public interface IDocument
{
    Task<Result<byte[], DocumentError>> GenerateAnnexesAsync(Guid userId, GenerateAnnexesRequest request);
    Task<Result<byte[], DocumentError>> GenerateAnnexesConsortiumAsync(
        Guid userId,
        GenerateAnnexesConsortiumRequest request);
}