namespace Infrastructure.Core.Services.Account;

public static class UserRepositorySql
{
    public const string GetUserByEmail = """
                             SELECT
                                 "UserId", "Email", "PasswordHash", "FullName",
                                 "IsActive", "CreatedDate", "UpdatedDate",
                                 "RefreshToken", "RefreshTokenExpirationDate"
                             FROM "Auth"."Users"
                             WHERE "Email" = @Email
                             """;
    
    public const string GetById = """
                                SELECT
                                    "UserId", "Email", "PasswordHash", "FullName",
                                    "IsActive", "CreatedDate", "UpdatedDate",
                                    "RefreshToken", "RefreshTokenExpirationDate"
                                FROM "Auth"."Users"
                                WHERE "UserId" = @UserId
                                """;
    
    public const string GetByRefreshToken = """
                                            SELECT
                                                "UserId", "Email", "PasswordHash", "FullName",
                                                "IsActive", "CreatedDate", "UpdatedDate",
                                                "RefreshToken", "RefreshTokenExpirationDate"
                                            FROM "Auth"."Users"
                                            WHERE "RefreshToken" = @RefreshToken
                                                AND "RefreshTokenExpirationDate" > @CurrentDate
                                            """;
    
    public const string UpdateRefreshToken = """
                                             UPDATE "Auth"."Users"
                                             SET "RefreshToken" = @RefreshToken,
                                                 "RefreshTokenExpirationDate" = @ExpirationDate,
                                                 "UpdatedDate" = @UpdatedDate
                                             WHERE "UserId" = @UserId
                                             """;
    
    public const string DeleteRefreshToken = """
                                                  UPDATE "Auth"."Users"
                                                  SET "RefreshToken" = NULL,
                                                      "RefreshTokenExpirationDate" = NULL,
                                                      "UpdatedDate" = @UpdatedDate
                                                  WHERE "UserId" = @UserId
                                                  """;
    
    public const string GetUserFirstCompany = """
                                           SELECT
                                               c."CompanyId", c."Ruc", c."RazonSocial", c."DomicilioLegal",
                                               c."Telefono", c."Email", c."IsMype", c."CreatedDate", c."UpdatedDate"
                                           FROM "Company"."Companies" c
                                           INNER JOIN "Company"."UserCompanies" uc ON c."CompanyId" = uc."CompanyId"
                                           WHERE uc."UserId" = @UserId
                                               AND uc."IsActive" = TRUE
                                           ORDER BY uc."CreatedDate" ASC
                                           LIMIT 1
                                           """;
    
    public const string ValidateUserCompanyOwnership = """
                                                      SELECT EXISTS (
                                                          SELECT 1 
                                                          FROM "Company"."UserCompanies" 
                                                          WHERE "UserId" = @UserId 
                                                            AND "CompanyId" = @CompanyId 
                                                            AND "IsActive" = TRUE
                                                      )
                                                      """;
}