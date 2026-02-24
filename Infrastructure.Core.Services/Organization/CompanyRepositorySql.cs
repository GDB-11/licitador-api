namespace Infrastructure.Core.Services.Organization;

public static class CompanyRepositorySql
{
    public const string GetCompanyDetails = """
                                            SELECT 
                                                c."CompanyId", c."Ruc", c."RazonSocial", c."DomicilioLegal", 
                                                c."Telefono", c."Email", c."FechaConstitucion", c."IsMype", c."CreatedDate", c."UpdatedDate",
                                                lr."LegalRepresentativeId", lr."CompanyId" AS "LRCompanyId", lr."FullName",
                                                lr."DocumentType", lr."DocumentNumber", lr."PowerRegistrationLocation", 
                                                lr."PowerRegistrationSheet", lr."PowerRegistrationEntry", lr."NationalIdImage", 
                                                lr."IsActive" AS "LRIsActive", lr."CreatedDate" AS "LRCreatedDate", 
                                                lr."UpdatedDate" AS "LRUpdatedDate",
                                                ba."BankAccountId", ba."CompanyId" AS "BACompanyId", ba."BankName", 
                                                ba."AccountNumber", ba."CciCode", ba."IsActive" AS "BAIsActive",
                                                ba."CreatedDate" AS "BACreatedDate", ba."UpdatedDate" AS "BAUpdatedDate"
                                            FROM "Company"."Companies" c
                                            LEFT JOIN LATERAL (
                                                SELECT * 
                                                FROM "Company"."LegalRepresentatives" 
                                                WHERE "CompanyId" = c."CompanyId" 
                                                  AND "IsActive" = TRUE 
                                                ORDER BY "CreatedDate" DESC 
                                                LIMIT 1
                                            ) lr ON TRUE
                                            LEFT JOIN LATERAL (
                                                SELECT * 
                                                FROM "Company"."BankAccounts" 
                                                WHERE "CompanyId" = c."CompanyId" 
                                                  AND "IsActive" = TRUE 
                                                ORDER BY "CreatedDate" DESC 
                                                LIMIT 1
                                            ) ba ON TRUE
                                            WHERE c."CompanyId" = @CompanyId
                                            """;

    public const string GetCompanyDetailsByConsortiumCompanyId = """
                                                                  SELECT 
                                                                      c."CompanyId", c."Ruc", c."RazonSocial", c."DomicilioLegal", 
                                                                      c."Telefono", c."Email", c."FechaConstitucion", c."IsMype", c."CreatedDate", c."UpdatedDate",
                                                                      lr."LegalRepresentativeId", lr."CompanyId" AS "LRCompanyId", lr."FullName", 
                                                                      lr."DocumentType", lr."DocumentNumber", lr."PowerRegistrationLocation", 
                                                                      lr."PowerRegistrationSheet", lr."PowerRegistrationEntry", lr."NationalIdImage", 
                                                                      lr."IsActive" AS "LRIsActive", lr."CreatedDate" AS "LRCreatedDate", 
                                                                      lr."UpdatedDate" AS "LRUpdatedDate",
                                                                      ba."BankAccountId", ba."CompanyId" AS "BACompanyId", ba."BankName", 
                                                                      ba."AccountNumber", ba."CciCode", ba."IsActive" AS "BAIsActive",
                                                                      ba."CreatedDate" AS "BACreatedDate", ba."UpdatedDate" AS "BAUpdatedDate"
                                                                  FROM "Consortium"."ConsortiumCompanies" cc
                                                                  INNER JOIN "Company"."Companies" c ON c."CompanyId" = cc."CompanyId"
                                                                  LEFT JOIN LATERAL (
                                                                      SELECT * 
                                                                      FROM "Consortium"."ConsortiumCompanyLegalRepresentatives" 
                                                                      WHERE "ConsortiumCompanyId" = cc."ConsortiumCompanyId" 
                                                                        AND "IsActive" = TRUE 
                                                                      ORDER BY "CreatedDate" DESC 
                                                                      LIMIT 1
                                                                  ) lr ON TRUE
                                                                  LEFT JOIN LATERAL (
                                                                      SELECT * 
                                                                      FROM "Company"."BankAccounts" 
                                                                      WHERE "CompanyId" = c."CompanyId" 
                                                                        AND "IsActive" = TRUE 
                                                                      ORDER BY "CreatedDate" DESC 
                                                                      LIMIT 1
                                                                  ) ba ON TRUE
                                                                  WHERE cc."ConsortiumCompanyId" = @ConsortiumCompanyId
                                                                  """;

    public const string UpdateCompany = """
                                        UPDATE "Company"."Companies"
                                        SET "Ruc" = @Ruc,
                                            "RazonSocial" = @RazonSocial,
                                            "DomicilioLegal" = @DomicilioLegal,
                                            "Telefono" = @Telefono,
                                            "Email" = @Email,
                                            "FechaConstitucion" = @FechaConstitucion,
                                            "IsMype" = @IsMype,
                                            "UpdatedDate" = @UpdatedDate
                                        WHERE "CompanyId" = @CompanyId
                                        """;
    
    public const string GetActiveLegalRepresentativeId = """
                                                         SELECT "LegalRepresentativeId"
                                                         FROM "Company"."LegalRepresentatives"
                                                         WHERE "CompanyId" = @CompanyId
                                                           AND "IsActive" = TRUE
                                                         ORDER BY "CreatedDate" DESC
                                                         LIMIT 1
                                                         """;
    
    public const string UpdateLegalRepresentative = """
                                                    UPDATE "Company"."LegalRepresentatives"
                                                    SET "FullName" = @FullName,
                                                        "DocumentType" = @DocumentType,
                                                        "DocumentNumber" = @DocumentNumber,
                                                        "NationalIdImage" = @NationalIdImage,
                                                        "UpdatedDate" = @UpdatedDate
                                                    WHERE "LegalRepresentativeId" = @LegalRepresentativeId
                                                    """;
    
    public const string InsertLegalRepresentative = """
                                                    INSERT INTO "Company"."LegalRepresentatives" (
                                                        "LegalRepresentativeId", "CompanyId", "FullName", "DocumentType", "DocumentNumber",
                                                        "NationalIdImage", "IsActive", "CreatedDate"
                                                    )
                                                    VALUES (
                                                        @LegalRepresentativeId, @CompanyId, @FullName, @DocumentType, @DocumentNumber,
                                                        @NationalIdImage, TRUE, @CreatedDate
                                                    )
                                                    RETURNING "LegalRepresentativeId"
                                                    """;
    
    public const string GetActiveBankAccountId = """
                                                 SELECT "BankAccountId"
                                                 FROM "Company"."BankAccounts"
                                                 WHERE "CompanyId" = @CompanyId
                                                   AND "IsActive" = TRUE
                                                 ORDER BY "CreatedDate" DESC
                                                 LIMIT 1
                                                 """;
    
    public const string UpdateBankAccount = """
                                            UPDATE "Company"."BankAccounts"
                                            SET "BankName" = @BankName,
                                                "AccountNumber" = @AccountNumber,
                                                "CciCode" = @CciCode,
                                                "UpdatedDate" = @UpdatedDate
                                            WHERE "BankAccountId" = @BankAccountId
                                            """;
    
    public const string InsertBankAccount = """
                                            INSERT INTO "Company"."BankAccounts" (
                                                "BankAccountId", "CompanyId", "BankName", "AccountNumber", "CciCode",
                                                "IsActive", "CreatedDate"
                                            )
                                            VALUES (
                                                @BankAccountId, @CompanyId, @BankName, @AccountNumber, @CciCode,
                                                TRUE, @CreatedDate
                                            )
                                            RETURNING "BankAccountId"
                                            """;
}