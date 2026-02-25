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
            var memberCount = request.Members.Count;

            if (memberCount < 2 || memberCount > 3)
                return Result<byte[], DocumentError>.Failure(
                    new DocumentValidationError("El consorcio debe tener 2 o 3 empresas participantes"));

            // Solo se necesita la empresa propia si el usuario es líder o algún miembro es empresa propia
            var needsOwnCompany = request.IsOwnCompanyLeader 
                                  || request.Members.Any(m => m.EsEmpresaPropia);

            CompanyDetails? ownCompanyDetails = null;

            if (needsOwnCompany)
            {
                var ownCompanyResult = await GetCompanyDetailsByUserIdAsync(userId);
                if (ownCompanyResult.IsFailure)
                    return Result<byte[], DocumentError>.Failure(ownCompanyResult.Error);

                var validationError = ValidateCompanyDetails(ownCompanyResult.Value);
                if (validationError != null)
                    return Result<byte[], DocumentError>.Failure(validationError);

                ownCompanyDetails = ownCompanyResult.Value;
            }

            // Resolver líder
            var leaderResult = await ResolveLeaderAsync(request, ownCompanyDetails);
            if (leaderResult.IsFailure)
                return Result<byte[], DocumentError>.Failure(leaderResult.Error);

            // Resolver miembros
            var resolvedMembersResult = await ResolveMembersAsync(request.Members, ownCompanyDetails);
            if (resolvedMembersResult.IsFailure)
                return Result<byte[], DocumentError>.Failure(resolvedMembersResult.Error);

            // Cargar plantilla
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

            var replacements = BuildConsortiumReplacements(request, leaderResult.Value, resolvedMembersResult.Value);

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

    private async Task<Result<CompanyDetails, DocumentError>> ResolveLeaderAsync(
        GenerateAnnexesConsortiumRequest request,
        CompanyDetails? ownCompanyDetails)
    {
        if (request.IsOwnCompanyLeader)
        {
            // ownCompanyDetails está garantizado no-null si IsOwnCompanyLeader = true
            return Result<CompanyDetails, DocumentError>.Success(ownCompanyDetails!);
        }

        if (string.IsNullOrEmpty(request.LeaderConsortiumCompanyId))
            return Result<CompanyDetails, DocumentError>.Failure(
                new DocumentValidationError(
                    "Se debe especificar LeaderConsortiumCompanyId cuando la empresa propia no es el líder"));

        var leaderResult = await _companyRepository
            .GetCompanyDetailsByConsortiumCompanyIdAsync(request.LeaderConsortiumCompanyId);

        if (leaderResult.IsFailure)
            return Result<CompanyDetails, DocumentError>.Failure(new DocumentGenerationError(leaderResult.Error.Message));

        if (leaderResult.Value is null)
            return Result<CompanyDetails, DocumentError>.Failure(
                new DocumentGenerationError(
                    $"No se encontraron datos para la empresa líder (Id: {request.LeaderConsortiumCompanyId})"));

        return Result<CompanyDetails, DocumentError>.Success(leaderResult.Value);
    }

    private async Task<Result<List<CompanyDetails>, DocumentError>> ResolveMembersAsync(
        List<ConsortiumMember> members,
        CompanyDetails? ownCompanyDetails)
    {
        var resolved = new List<CompanyDetails>(members.Count);

        for (int i = 0; i < members.Count; i++)
        {
            var member = members[i];

            if (member.EsEmpresaPropia)
            {
                // ownCompanyDetails está garantizado no-null si hay algún EsEmpresaPropia = true
                resolved.Add(ownCompanyDetails!);
                continue;
            }

            if (string.IsNullOrEmpty(member.ConsortiumCompanyId))
                return Result<List<CompanyDetails>, DocumentError>.Failure(
                    new DocumentValidationError(
                        $"El miembro {i + 1} no tiene ConsortiumCompanyId y no está marcado como empresa propia"));

            var memberResult = await _companyRepository
                .GetCompanyDetailsByConsortiumCompanyIdAsync(member.ConsortiumCompanyId);

            if (memberResult.IsFailure)
                return Result<List<CompanyDetails>, DocumentError>.Failure(new DocumentGenerationError(memberResult.Error.Message));

            if (memberResult.Value is null)
                return Result<List<CompanyDetails>, DocumentError>.Failure(
                    new DocumentGenerationError(
                        $"No se encontraron datos para el miembro {i + 1} (Id: {member.ConsortiumCompanyId})"));

            resolved.Add(memberResult.Value);
        }

        return Result<List<CompanyDetails>, DocumentError>.Success(resolved);
    }
    
    private Dictionary<string, string> BuildConsortiumReplacements(
    GenerateAnnexesConsortiumRequest request,
    CompanyDetails leaderDetails,
    List<CompanyDetails> members)
    {
        var now = DateTime.Now;
        var culture = new CultureInfo("es-PE");

        var replacements = new Dictionary<string, string>
        {
            ["{{ENTIDAD_CONTRATANTE}}"]                     = request.EntityName,
            ["{{NUMERO_PROCESO}}"]                          = request.LicitacionNumber,
            ["{{NOMBRE_CONSORCIO}}"]                        = request.ConsortiumName,
            ["{{CIUDAD}}"]                                  = request.City,
            ["{{FECHA}}"]                                   = now.ToString("dd/MM/yyyy", culture),

            ["{{NOMBRE_REPRESENTANTE_LEGAL_LIDER}}"]        = leaderDetails.LegalRepresentative?.FullName ?? "",
            ["{{TIPO_DOCUMENTO_REPRESENTANTE_LEGAL_LIDER}}"]= leaderDetails.LegalRepresentative?.DocumentType ?? "DNI",
            ["{{NUM_DOCUMENTO_REPRESENTANTE_LEGAL_LIDER}}"] = leaderDetails.LegalRepresentative?.DocumentNumber ?? "",

            ["{{RAZON_SOCIAL_EMPRESA_LIDER}}"]              = leaderDetails.Company.RazonSocial,
            ["{{NUMERO_FICHA}}"]                            = request.NumeroFicha ?? "",
            ["{{NUMERO_ASIENTO}}"]                          = request.NumeroAsiento ?? ""
        };

        for (int i = 0; i < members.Count; i++)
        {
            var details = members[i];
            int n = i + 1;

            string[] phones = details.Company.Telefono?
                .Split([',', ';', '/'], StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
                ?? [];

            replacements[$"{{{{RAZON_SOCIAL_EMPRESA_{n}}}}}"]    = details.Company.RazonSocial;
            replacements[$"{{{{DOMICILIO_LEGAL_EMPRESA_{n}}}}}"] = details.Company.DomicilioLegal;
            replacements[$"{{{{RUC_EMPRESA_{n}}}}}"]             = details.Company.Ruc;
            replacements[$"{{{{TEL1EMP_{n}}}}}"]                 = phones.Length > 0 ? phones[0] : "";
            replacements[$"{{{{TEL2EMP_{n}}}}}"]                 = phones.Length > 1 ? phones[1] : "";
            replacements[$"{{{{CORREO_EMPRESA_{n}}}}}"]          = details.Company.Email;
            replacements[$"{{{{MYPESI_E{n}}}}}"]                 = details.Company.IsMype ? "X" : " ";
            replacements[$"{{{{MYPENO_E{n}}}}}"]                 = details.Company.IsMype ? " " : "X";
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