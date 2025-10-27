using System.Globalization;
using Application.Core.DTOs.Document;
using Application.Core.Interfaces.Document;
using DocumentFormat.OpenXml;
using DocumentFormat.OpenXml.Packaging;
using DocumentFormat.OpenXml.Wordprocessing;
using Global.Helpers.Functional;
using Global.Objects.Document;
using Global.Objects.Results;
using Infrastructure.Core.Interfaces.Account;
using Infrastructure.Core.Models.Company;

namespace Application.Core.Services.Document;

public class DocumentService : IDocument
{
    private readonly WordTemplateService _templateService;
    private readonly IUserRepository _userRepository;
    
    public DocumentService(
        WordTemplateService templateService,
        IUserRepository userRepository)
    {
        _templateService = templateService;
        _userRepository = userRepository;
    }
    
    public async Task<Result<byte[], DocumentError>> GenerateAnnexesAsync(
        Guid userId, 
        GenerateAnnexesRequest request)
    {
        try
        {
            // Obtener datos de la empresa
            var companyDetailsResult = await GetCompanyDetailsByUserIdAsync(userId);
            
            if (companyDetailsResult.IsFailure)
            {
                return Result<byte[], DocumentError>.Failure(companyDetailsResult.Error);
            }
            
            var companyDetails = companyDetailsResult.Value;
            
            // Validar datos requeridos
            var validationError = ValidateCompanyDetails(companyDetails);
            if (validationError != null)
            {
                return Result<byte[], DocumentError>.Failure(validationError);
            }
            
            // Convertir template de Base64 a bytes
            byte[] templateBytes;
            try
            {
                templateBytes = Convert.FromBase64String(DocumentTemplates.AnexoIndividualTemplate);
            }
            catch (FormatException ex)
            {
                return Result<byte[], DocumentError>.Failure(
                    new DocumentGenerationError("Error al cargar la plantilla del documento", ex));
            }
            
            // Crear diccionario de reemplazos
            var replacements = BuildReplacements(request, companyDetails);
            
            // Generar documento
            byte[] documentBytes;
            try
            {
                documentBytes = _templateService.FillTemplate(templateBytes, replacements);
            }
            catch (Exception ex)
            {
                return Result<byte[], DocumentError>.Failure(
                    new DocumentGenerationError("Error al procesar la plantilla del documento", ex));
            }
            
            return Result<byte[], DocumentError>.Success(documentBytes);
        }
        catch (Exception ex)
        {
            return Result<byte[], DocumentError>.Failure(
                new DocumentGenerationError("Error inesperado al generar el documento", ex));
        }
    }
    
    private async Task<Result<CompanyDetails, DocumentError>> GetCompanyDetailsByUserIdAsync(Guid userId)
    {
        try
        {
            var company = await _userRepository.GetUserFirstCompanyAsync(userId);
            var companyDetails = await _userRepository.GetCompanyDetailsAsync(company.Value.CompanyId);
            
            if (companyDetails.Value is null)
            {
                return Result<CompanyDetails, DocumentError>.Failure(
                    new DocumentCompanyNotFoundError());
            }
            
            return Result<CompanyDetails, DocumentError>.Success(companyDetails.Value);
        }
        catch (Exception ex)
        {
            return Result<CompanyDetails, DocumentError>.Failure(
                new DocumentRepositoryError("Error al obtener los datos de la empresa", ex));
        }
    }
    
    private DocumentValidationError? ValidateCompanyDetails(CompanyDetails companyDetails)
    {
        var errors = new List<string>();
        
        if (string.IsNullOrWhiteSpace(companyDetails.Company.RazonSocial))
            errors.Add("La razón social es requerida");
        
        if (string.IsNullOrWhiteSpace(companyDetails.Company.Ruc))
            errors.Add("El RUC es requerido");
        
        if (string.IsNullOrWhiteSpace(companyDetails.Company.DomicilioLegal))
            errors.Add("El domicilio legal es requerido");
        
        if (string.IsNullOrWhiteSpace(companyDetails.Company.Email))
            errors.Add("El email es requerido");
        
        if (companyDetails.LegalRepresentative == null)
            errors.Add("El representante legal es requerido");
        else
        {
            if (string.IsNullOrWhiteSpace(companyDetails.LegalRepresentative.FullName))
                errors.Add("El nombre del representante legal es requerido");
            
            if (string.IsNullOrWhiteSpace(companyDetails.LegalRepresentative.DocumentNumber))
                errors.Add("El número de documento del representante legal es requerido");
        }
        
        return errors.Any() 
            ? new DocumentValidationError(string.Join(", ", errors))
            : null;
    }
    
    private Dictionary<string, string> BuildReplacements(
        GenerateAnnexesRequest request, 
        CompanyDetails companyDetails)
    {
        var now = DateTime.Now;
        var culture = new CultureInfo("es-PE");
        
        // Procesar teléfonos
        var phones = companyDetails.Company.Telefono?
            .Split(new[] { ',', ';', '/' }, StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
            ?? Array.Empty<string>();
        
        var replacements = new Dictionary<string, string>
        {
            // Datos de la licitación
            ["{{NUMERO_PROCESO}}"] = request.LicitacionNumber,
            ["{{CIUDAD}}"] = request.City,
            
            // Datos de fecha
            ["{{FECHA}}"] = now.ToString("dd/MM/yyyy", culture),
            ["{{DIA_DEL_MES}}"] = now.Day.ToString(),
            ["{{NOMBRE_DEL_MES}}"] = culture.DateTimeFormat.GetMonthName(now.Month),
            
            // Datos de la empresa
            ["{{RAZON_SOCIAL}}"] = companyDetails.Company.RazonSocial,
            ["{{RUC}}"] = companyDetails.Company.Ruc,
            ["{{DOMICILIO_LEGAL}}"] = companyDetails.Company.DomicilioLegal,
            ["{{EMAIL}}"] = companyDetails.Company.Email,
            
            // Teléfonos (solo usar el primero, segundo vacío)
            ["{{TELEFONO_1}}"] = phones.Length > 0 ? phones[0] : "",
            ["{{ TELEFONO_2}}"] = "", // Nota: tiene un espacio extra en el template
            
            // MYPE - X en el campo correspondiente
            ["{{ES}}"] = companyDetails.Company.IsMype ? "X" : " ",
            ["{{NO_ES}}"] = companyDetails.Company.IsMype ? " " : "X",
            
            // Datos del representante legal
            ["{{NOMBRE_REPRESENTANTE_LEGAL}}"] = companyDetails.LegalRepresentative?.FullName ?? "",
            ["{{TIPO_DOC_REPRESENTANTE_LEGAL}}"] = companyDetails.LegalRepresentative?.DocumentType ?? "DNI",
            ["{{NUM_DOC_REPRESENTANTE_LEGAL}}"] = companyDetails.LegalRepresentative?.DocumentNumber ?? "",
            
            // Datos de poder
            ["{{NUMERO_FICHA}}"] = companyDetails.LegalRepresentative?.PowerRegistrationSheet ?? "",
            ["{{NUMERO_ASIENTO}}"] = companyDetails.LegalRepresentative?.PowerRegistrationEntry ?? ""
        };
        
        return replacements;
    }
}