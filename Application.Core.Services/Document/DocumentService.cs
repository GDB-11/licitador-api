using System.Globalization;
using Application.Core.DTOs.Document.Errors;
using Application.Core.DTOs.Document.Request;
using Application.Core.DTOs.Document.Templates;
using Application.Core.Interfaces.Document;
using BindSharp;
using Infrastructure.Core.Interfaces.Account;
using Infrastructure.Core.Interfaces.Organization;
using Infrastructure.Core.Models.Company;

namespace Application.Core.Services.Document;

public class DocumentService : IDocument
{
    private readonly WordTemplateService _templateService;
    private readonly IUserRepository _userRepository;
    private readonly ICompanyRepository _companyRepository;
    
    public DocumentService(
        WordTemplateService templateService,
        IUserRepository userRepository,
        ICompanyRepository companyRepository)
    {
        _templateService = templateService;
        _userRepository = userRepository;
        _companyRepository = companyRepository;
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
            
            return documentBytes;
        }
        catch (Exception ex)
        {
            return new DocumentGenerationError("Error inesperado al generar el documento", ex);
        }
    }

    public async Task<Result<byte[], DocumentError>> GenerateAnnexesConsortiumAsync(
        Guid userId,
        GenerateAnnexesConsortiumRequest request)
    {
        try
        {
            var consortiumMembers = request.Members;
            var memberCount = consortiumMembers.Count;

            if (memberCount < 2 || memberCount > 3)
            {
                return Result<byte[], DocumentError>.Failure(
                    new DocumentValidationError("El consorcio debe tener 2 o 3 empresas participantes"));
            }

            var leaderCompanyDetails = await GetCompanyDetailsByUserIdAsync(userId);

            if (leaderCompanyDetails.IsFailure)
            {
                return Result<byte[], DocumentError>.Failure(leaderCompanyDetails.Error);
            }

            var validationError = ValidateCompanyDetails(leaderCompanyDetails.Value);
            if (validationError != null)
            {
                return Result<byte[], DocumentError>.Failure(validationError);
            }

            byte[] templateBytes;
            try
            {
                var templateBase64 = memberCount == 2 
                    ? DocumentTemplates.AnexoConsorcioDosParticipantesTemplate 
                    : DocumentTemplates.AnexoConsorcioTresParticipantesTemplate;

                templateBytes = Convert.FromBase64String(templateBase64);
            }
            catch (FormatException ex)
            {
                return Result<byte[], DocumentError>.Failure(
                    new DocumentGenerationError("Error al cargar la plantilla del documento", ex));
            }

            var replacements = await BuildConsortiumReplacements(request, leaderCompanyDetails.Value, consortiumMembers);

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

            return documentBytes;
        }
        catch (Exception ex)
        {
            return new DocumentGenerationError("Error inesperado al generar el documento", ex);
        }
    }

    private async Task<Dictionary<string, string>> BuildConsortiumReplacements(
        GenerateAnnexesConsortiumRequest request,
        CompanyDetails leaderCompanyDetails,
        List<ConsortiumMember> members)
    {
        var now = DateTime.Now;
        var culture = new CultureInfo("es-PE");

        var replacements = new Dictionary<string, string>
        {
            ["{{NUMERO_PROCESO}}"] = request.LicitacionNumber,
            ["{{NOMBRE_CONSORCIO}}"] = request.ConsortiumName,
            ["{{CIUDAD}}"] = request.City,
            ["{{FECHA}}"] = now.ToString("dd/MM/yyyy", culture),

            ["{{NOMBRE_REPRESENTANTE_LEGAL_LIDER}}"] = leaderCompanyDetails.LegalRepresentative?.FullName ?? "",
            ["{{TIPO_DOCUMENTO_REPRESENTANTE_LEGAL_LIDER}}"] = leaderCompanyDetails.LegalRepresentative?.DocumentType ?? "DNI",
            ["{{NUM_DOCUMENTO_REPRESENTANTE_LEGAL_LIDER}}"] = leaderCompanyDetails.LegalRepresentative?.DocumentNumber ?? "",

            ["{{RAZON_SOCIAL_EMPRESA_LIDER}}"] = leaderCompanyDetails.Company.RazonSocial,
            ["{{NUMERO_FICHA}}"] = request.NumeroFicha ?? "",
            ["{{NUMERO_ASIENTO}}"] = request.NumeroAsiento ?? ""
        };

        for (int i = 0; i < members.Count; i++)
        {
            var member = members[i];
            int enterpriseNumber = i + 1;

            CompanyDetails memberDetails;

            if (member.EsEmpresaPropia)
            {
                memberDetails = leaderCompanyDetails;
            }
            else
            {
                if (string.IsNullOrEmpty(member.ConsortiumCompanyId))
                {
                    continue;
                }

                var companyData = await _companyRepository.GetCompanyDetailsByConsortiumCompanyIdAsync(member.ConsortiumCompanyId);

                if (companyData.IsFailure || companyData.Value is null)
                {
                    continue;
                }

                memberDetails = companyData.Value;
            }

            string[] phones = memberDetails.Company.Telefono?
                                  .Split(new[] { ',', ';', '/' }, StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
                              ?? Array.Empty<string>();

            replacements[$"{{{{RAZON_SOCIAL_EMPRESA_{enterpriseNumber}}}}}"] = memberDetails.Company.RazonSocial;
            replacements[$"{{{{DOMICILIO_LEGAL_EMPRESA_{enterpriseNumber}}}}}"] = memberDetails.Company.DomicilioLegal;
            replacements[$"{{{{RUC_EMPRESA_{enterpriseNumber}}}}}"] = memberDetails.Company.Ruc;
            replacements[$"{{{{TEL1EMP_{enterpriseNumber}}}}}"] = phones.Length > 0 ? phones[0] : "";
            replacements[$"{{{{TEL2EMP_{enterpriseNumber}}}}}"] = phones.Length > 1 ? phones[1] : "";
            replacements[$"{{{{CORREO_EMPRESA_{enterpriseNumber}}}}}"] = memberDetails.Company.Email;
            replacements[$"{{{{MYPESI_E{enterpriseNumber}}}}}"] = memberDetails.Company.IsMype ? "X" : " ";
            replacements[$"{{{{MYPENO_E{enterpriseNumber}}}}}"] = memberDetails.Company.IsMype ? " " : "X";
        }

        return replacements;
    }
    
    private async Task<Result<CompanyDetails, DocumentError>> GetCompanyDetailsByUserIdAsync(Guid userId)
    {
        try
        {
            var company = await _userRepository.GetUserFirstCompanyAsync(userId);
            var companyDetails = await _companyRepository.GetCompanyDetailsAsync(company.Value.CompanyId);
            
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
        string[] phones = companyDetails.Company.Telefono?
                              .Split(new[] { ',', ';', '/' }, StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
                          ?? Array.Empty<string>();
        
        var replacements = new Dictionary<string, string>
        {
            // Datos de la licitación
            ["{{NUMERO_PROCESO}}"] = request.LicitacionNumber,
            ["{{ENTIDAD_CONTRATANTE}}"] = request.LicitacionNumber,
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
            ["{{NUMERO_FICHA}}"] = request.NumeroFicha ?? "",
            ["{{NUMERO_ASIENTO}}"] = request.NumeroAsiento ?? ""
        };
        
        return replacements;
    }
}