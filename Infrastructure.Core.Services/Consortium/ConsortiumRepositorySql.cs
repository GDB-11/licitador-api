namespace Infrastructure.Core.Services.Consortium;

public static class ConsortiumRepositorySql
{
	public const string GetAllConsortiumCompanies = """
	                        SELECT
	                        	cco."ConsortiumCompanyId"
	                        	, cco."CompanyId"
	                        	, cco."Ruc"
	                        	, cco."RnpRegistration"
	                        	, cco."RazonSocial"
	                        	, cco."NombreComercial"
	                        	, cco."RnpValidUntil"
	                            , cco."MainActivity"
	                            , cco."DomicilioFiscal"    
	                            , cco."ContactPhone"
	                            , cco."ContactEmail"
	                            , cco."IsActive"
	                            , cco."CreatedDate"
	                            , cco."UpdatedDate"
	                            , clr."ConsortiumLegalRepresentativeId"
	                            , clr."ConsortiumCompanyId"
	                            , clr."Dni"
	                            , clr."FullName"
	                            , clr."Position"
	                            , clr."IsActive"
	                            , clr."CreatedDate"
	                            , clr."UpdatedDate"
	                        FROM
	                        	"Company"."ConsortiumCompany" cco
	                        	LEFT JOIN "Company"."ConsortiumLegalRepresentative" clr ON cco."ConsortiumCompanyId" = clr."ConsortiumCompanyId"
	                        WHERE
	                            cco."CompanyId" = @CompanyId
	                        """;
	
	public const string GetNumberOfActiveConsortiumCompanies = """
	                                                     SELECT
	                                                         COUNT(*)
	                                                     FROM
	                                                         "Company"."ConsortiumCompany" cco
	                                                     WHERE
	                                                         cco."CompanyId" = @CompanyId
	                                                         AND cco."IsActive" = TRUE
	                                                     """;
	
	public const string ValidateCompanyConsortiumOwnership = """
	                                                   SELECT EXISTS (
	                                                       SELECT 1 
	                                                       FROM "Company"."ConsortiumCompany" 
	                                                       WHERE cco."CompanyId" = @CompanyId 
	                                                         AND "ConsortiumCompanyId" = @ConsortiumCompanyId 
	                                                         AND "IsActive" = TRUE
	                                                   )
	                                                   """;
	
	public const string GetConsortiumCompany = """
	                                                SELECT
	                                                	cco."ConsortiumCompanyId"
	                                                	, cco."CompanyId"
	                                                	, cco."Ruc"
	                                                	, cco."RnpRegistration"
	                                                	, cco."RazonSocial"
	                                                	, cco."NombreComercial"
	                                                	, cco."RnpValidUntil"
	                                                    , cco."MainActivity"
	                                                    , cco."DomicilioFiscal"    
	                                                    , cco."ContactPhone"
	                                                    , cco."ContactEmail"
	                                                    , cco."IsActive"
	                                                    , cco."CreatedDate"
	                                                    , cco."UpdatedDate"
	                                                    , clr."ConsortiumLegalRepresentativeId"
	                                                    , clr."ConsortiumCompanyId"
	                                                    , clr."Dni"
	                                                    , clr."FullName"
	                                                    , clr."Position"
	                                                    , clr."IsActive"
	                                                    , clr."CreatedDate"
	                                                    , clr."UpdatedDate"
	                                                FROM
	                                                	"Company"."ConsortiumCompany" cco
	                                                	LEFT JOIN "Company"."ConsortiumLegalRepresentative" clr ON cco."ConsortiumCompanyId" = clr."ConsortiumCompanyId"
	                                                WHERE
	                                                    cco."ConsortiumCompanyId" = @ConsortiumCompanyId
	                                                """;

	public const string InsertConsortiumCompany = """
	                                              INSERT INTO "Company"."ConsortiumCompany"
	                                              	("ConsortiumCompanyId", "CompanyId", "Ruc", "RnpRegistration", "RazonSocial", "NombreComercial", "RnpValidUntil", "MainActivity", "DomicilioFiscal", "ContactPhone", "ContactEmail")
	                                              VALUES
	                                              	(@ConsortiumCompanyId, @CompanyId, @Ruc, @RnpRegistration, @RazonSocial, @NombreComercial, @RnpValidUntil, @MainActivity, @DomicilioFiscal, @ContactPhone, @ContactEmail)
	                                              """;

	public const string UpdateConsortiumCompany = """
	                                              UPDATE "Company"."ConsortiumCompany"
	                                              SET
	                                              	"Ruc" = @Ruc,
	                                              	"RnpRegistration" = @RnpRegistration,
	                                              	"RazonSocial" = @RazonSocial,
	                                              	"NombreComercial" = @NombreComercial,
	                                              	"RnpValidUntil" = @RnpValidUntil,
	                                              	"MainActivity" = @MainActivity,
	                                              	"DomicilioFiscal" = @DomicilioFiscal,
	                                              	"ContactPhone" = @ContactPhone,
	                                              	"ContactEmail" = @ContactEmail
	                                              WHERE
	                                              	"ConsortiumCompanyId" = @ConsortiumCompanyId
	                                              """;

	public const string InsertConsortiumLegalRepresentative = """
	                                                          INSERT INTO "Company"."ConsortiumLegalRepresentative"
	                                                          	("ConsortiumLegalRepresentativeId", "ConsortiumCompanyId", "Dni", "FullName", "Position")
	                                                          VALUES
	                                                          	(@ConsortiumLegalRepresentativeId, @ConsortiumCompanyId, @Dni, @FullName, @Position)
	                                                          """;

	public const string UpdateConsortiumLegalRepresentative = """
	                                                          UPDATE "Company"."ConsortiumLegalRepresentative"
	                                                          SET
	                                                          	"Dni" = @Dni,
	                                                          	"FullName" = @FullName,
	                                                          	"Position" = @Position
	                                                          WHERE
	                                                          	"ConsortiumLegalRepresentativeId" = @ConsortiumLegalRepresentativeId
	                                                          """;

	public const string DeleteConsortiumCompany = """
	                                              DELETE FROM
	                                              	"Company"."ConsortiumCompany"
	                                              WHERE
	                                                  "ConsortiumCompanyId" = @ConsortiumCompanyId
	                                              """;
	
	public const string DeleteConsortiumLegalRepresentative = """
	                                                          DELETE FROM
	                                                          	"Company"."ConsortiumLegalRepresentative"
	                                                         WHERE
	                                                             "ConsortiumCompanyId" = @ConsortiumCompanyId
	                                                         """;
}