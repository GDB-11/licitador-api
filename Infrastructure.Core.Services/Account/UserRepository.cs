using Dapper;
using Global.Helpers.Functional;
using Global.Objects.Errors;
using Global.Objects.Functional;
using Global.Objects.Results;
using Infrastructure.Core.Interfaces.Account;
using Infrastructure.Core.Models.Account;
using Infrastructure.Core.Models.Company;
using System.Data;

namespace Infrastructure.Core.Services.Account;

public sealed class UserRepository : IUserRepository
{
    private readonly IDbConnection _connection;

    public UserRepository(IDbConnection connection)
    {
        _connection = connection;
    }

    public Task<Result<User?, GenericError>> GetByEmailAsync(string email) =>
        ResultExtensions.TryAsync(
            operation: () => ExecuteQueryByEmailAsync(email),
            errorMessage: "An unexpected error occurred while retrieving the user."
        )
        .MapErrorAsync(error => new GenericError(error));

    public Task<Result<User?, GenericError>> GetByIdAsync(Guid userId) =>
        ResultExtensions.TryAsync(
            operation: () => ExecuteQueryByIdAsync(userId),
            errorMessage: "An unexpected error occurred while retrieving the user."
        )
        .MapErrorAsync(error => new GenericError(error));

    public Task<Result<User?, GenericError>> GetByRefreshTokenAsync(string refreshToken) =>
        ResultExtensions.TryAsync(
            operation: () => ExecuteQueryByRefreshTokenAsync(refreshToken),
            errorMessage: "An unexpected error occurred while retrieving the user by refresh token."
        )
        .MapErrorAsync(error => new GenericError(error));

    public Task<Result<Unit, GenericError>> UpdateRefreshTokenAsync(Guid userId, string refreshToken, DateTime expirationDate) =>
        ResultExtensions.TryAsync(
            operation: () => ExecuteUpdateRefreshTokenAsync(userId, refreshToken, expirationDate),
            errorMessage: "An unexpected error occurred while updating the refresh token."
        )
        .MapErrorAsync(error => new GenericError(error));

    public Task<Result<Unit, GenericError>> ClearRefreshTokenAsync(Guid userId) =>
        ResultExtensions.TryAsync(
            operation: () => ExecuteClearRefreshTokenAsync(userId),
            errorMessage: "An unexpected error occurred while clearing the refresh token."
        )
        .MapErrorAsync(error => new GenericError(error));

    public Task<Result<Company?, GenericError>> GetUserFirstCompanyAsync(Guid userId) =>
        ResultExtensions.TryAsync(
            operation: () => ExecuteQueryUserFirstCompanyAsync(userId),
            errorMessage: "An unexpected error occurred while retrieving the user's company."
        )
        .MapErrorAsync(error => new GenericError(error));

    public Task<Result<bool, GenericError>> ValidateUserCompanyOwnershipAsync(Guid userId, Guid companyId) =>
        ResultExtensions.TryAsync(
            operation: () => ExecuteValidateUserCompanyOwnershipAsync(userId, companyId),
            errorMessage: "An unexpected error occurred while validating user company ownership."
        )
        .MapErrorAsync(error => new GenericError(error));

    public Task<Result<CompanyDetails?, GenericError>> GetCompanyDetailsAsync(Guid companyId) =>
        ResultExtensions.TryAsync(
            operation: () => ExecuteQueryCompanyDetailsAsync(companyId),
            errorMessage: "An unexpected error occurred while retrieving company details."
        )
        .MapErrorAsync(error => new GenericError(error));

    public Task<Result<Unit, GenericError>> UpdateCompanyAsync(
        Guid companyId,
        string ruc,
        string razonSocial,
        string domicilioLegal,
        string? telefono,
        string email,
        DateTime? fechaConstitucion,
        bool isMype) =>
        ResultExtensions.TryAsync(
            operation: () => ExecuteUpdateCompanyAsync(
                companyId,
                ruc,
                razonSocial,
                domicilioLegal,
                telefono,
                email,
                fechaConstitucion,
                isMype),
            errorMessage: "An unexpected error occurred while updating company."
        )
        .MapErrorAsync(error => new GenericError(error));

    public Task<Result<Guid?, GenericError>> GetActiveLegalRepresentativeIdAsync(Guid companyId) =>
        ResultExtensions.TryAsync(
            operation: () => ExecuteGetActiveLegalRepresentativeIdAsync(companyId),
            errorMessage: "An unexpected error occurred while retrieving legal representative ID."
        )
        .MapErrorAsync(error => new GenericError(error));

    public Task<Result<Unit, GenericError>> UpdateLegalRepresentativeAsync(
        Guid legalRepresentativeId,
        string fullName,
        string documentType,
        string documentNumber,
        byte[]? nationalIdImage) =>
        ResultExtensions.TryAsync(
            operation: () => ExecuteUpdateLegalRepresentativeAsync(
                legalRepresentativeId,
                fullName,
                documentType,
                documentNumber,
                nationalIdImage),
            errorMessage: "An unexpected error occurred while updating legal representative."
        )
        .MapErrorAsync(error => new GenericError(error));

    public Task<Result<Guid, GenericError>> InsertLegalRepresentativeAsync(
        Guid companyId,
        string fullName,
        string documentType,
        string documentNumber,
        byte[]? nationalIdImage) =>
        ResultExtensions.TryAsync(
            operation: () => ExecuteInsertLegalRepresentativeAsync(
                companyId,
                fullName,
                documentType,
                documentNumber,
                nationalIdImage),
            errorMessage: "An unexpected error occurred while inserting legal representative."
        )
        .MapErrorAsync(error => new GenericError(error));

    public Task<Result<Guid?, GenericError>> GetActiveBankAccountIdAsync(Guid companyId) =>
        ResultExtensions.TryAsync(
            operation: () => ExecuteGetActiveBankAccountIdAsync(companyId),
            errorMessage: "An unexpected error occurred while retrieving bank account ID."
        )
        .MapErrorAsync(error => new GenericError(error));

    public Task<Result<Unit, GenericError>> UpdateBankAccountAsync(
        Guid bankAccountId,
        string bankName,
        string accountNumber,
        string cciCode) =>
        ResultExtensions.TryAsync(
            operation: () => ExecuteUpdateBankAccountAsync(
                bankAccountId,
                bankName,
                accountNumber,
                cciCode),
            errorMessage: "An unexpected error occurred while updating bank account."
        )
        .MapErrorAsync(error => new GenericError(error));

    public Task<Result<Guid, GenericError>> InsertBankAccountAsync(
        Guid companyId,
        string bankName,
        string accountNumber,
        string cciCode) =>
        ResultExtensions.TryAsync(
            operation: () => ExecuteInsertBankAccountAsync(
                companyId,
                bankName,
                accountNumber,
                cciCode),
            errorMessage: "An unexpected error occurred while inserting bank account."
        )
        .MapErrorAsync(error => new GenericError(error));

    private async Task<User?> ExecuteQueryByEmailAsync(string email)
    {
        const string sql = """
            SELECT
                "UserId", "Email", "PasswordHash", "FullName",
                "IsActive", "CreatedDate", "UpdatedDate",
                "RefreshToken", "RefreshTokenExpirationDate"
            FROM "Auth"."Users"
            WHERE "Email" = @Email
            """;

        return await _connection.QuerySingleOrDefaultAsync<User>(
            sql,
            new { Email = email }
        );
    }

    private async Task<User?> ExecuteQueryByIdAsync(Guid userId)
    {
        const string sql = """
            SELECT
                "UserId", "Email", "PasswordHash", "FullName",
                "IsActive", "CreatedDate", "UpdatedDate",
                "RefreshToken", "RefreshTokenExpirationDate"
            FROM "Auth"."Users"
            WHERE "UserId" = @UserId
            """;

        return await _connection.QuerySingleOrDefaultAsync<User>(
            sql,
            new { UserId = userId }
        );
    }

    private async Task<User?> ExecuteQueryByRefreshTokenAsync(string refreshToken)
    {
        const string sql = """
            SELECT
                "UserId", "Email", "PasswordHash", "FullName",
                "IsActive", "CreatedDate", "UpdatedDate",
                "RefreshToken", "RefreshTokenExpirationDate"
            FROM "Auth"."Users"
            WHERE "RefreshToken" = @RefreshToken
                AND "RefreshTokenExpirationDate" > @CurrentDate
            """;

        return await _connection.QuerySingleOrDefaultAsync<User>(
            sql,
            new { RefreshToken = refreshToken, CurrentDate = DateTime.UtcNow }
        );
    }

    private async Task<Unit> ExecuteUpdateRefreshTokenAsync(Guid userId, string refreshToken, DateTime expirationDate)
    {
        const string sql = """
            UPDATE "Auth"."Users"
            SET "RefreshToken" = @RefreshToken,
                "RefreshTokenExpirationDate" = @ExpirationDate,
                "UpdatedDate" = @UpdatedDate
            WHERE "UserId" = @UserId
            """;

        await _connection.ExecuteAsync(
            sql,
            new
            {
                UserId = userId,
                RefreshToken = refreshToken,
                ExpirationDate = expirationDate,
                UpdatedDate = DateTime.UtcNow
            }
        );

        return Unit.Value;
    }

    private async Task<Unit> ExecuteClearRefreshTokenAsync(Guid userId)
    {
        const string sql = """
            UPDATE "Auth"."Users"
            SET "RefreshToken" = NULL,
                "RefreshTokenExpirationDate" = NULL,
                "UpdatedDate" = @UpdatedDate
            WHERE "UserId" = @UserId
            """;

        await _connection.ExecuteAsync(
            sql,
            new
            {
                UserId = userId,
                UpdatedDate = DateTime.UtcNow
            }
        );

        return Unit.Value;
    }

    private async Task<Company?> ExecuteQueryUserFirstCompanyAsync(Guid userId)
    {
        const string sql = """
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

        return await _connection.QuerySingleOrDefaultAsync<Company>(
            sql,
            new { UserId = userId }
        );
    }

    private async Task<bool> ExecuteValidateUserCompanyOwnershipAsync(Guid userId, Guid companyId)
    {
        const string sql = """
        SELECT EXISTS (
            SELECT 1 
            FROM "Company"."UserCompanies" 
            WHERE "UserId" = @UserId 
              AND "CompanyId" = @CompanyId 
              AND "IsActive" = TRUE
        )
        """;

        return await _connection.ExecuteScalarAsync<bool>(
            sql,
            new { UserId = userId, CompanyId = companyId }
        );
    }

    private async Task<CompanyDetails?> ExecuteQueryCompanyDetailsAsync(Guid companyId)
    {
        const string sql = """
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

        CompanyDetails? result = null;

        await _connection.QueryAsync<Company, LegalRepresentative?, BankAccount?, CompanyDetails?>(
            sql,
            (company, legalRep, bankAccount) =>
            {
                result = new CompanyDetails
                {
                    Company = company,
                    LegalRepresentative = legalRep,
                    BankAccount = bankAccount
                };
                return result;
            },
            new { CompanyId = companyId },
            splitOn: "LegalRepresentativeId,BankAccountId"
        );

        return result;
    }

    private async Task<Unit> ExecuteUpdateCompanyAsync(
        Guid companyId,
        string ruc,
        string razonSocial,
        string domicilioLegal,
        string? telefono,
        string email,
        DateTime? fechaConstitucion,
        bool isMype)
    {
        const string sql = """
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

        await _connection.ExecuteAsync(
            sql,
            new
            {
                CompanyId = companyId,
                Ruc = ruc,
                RazonSocial = razonSocial,
                DomicilioLegal = domicilioLegal,
                Telefono = telefono,
                Email = email,
                FechaConstitucion = fechaConstitucion,
                IsMype = isMype,
                UpdatedDate = DateTime.UtcNow
            }
        );

        return Unit.Value;
    }

    private async Task<Guid?> ExecuteGetActiveLegalRepresentativeIdAsync(Guid companyId)
    {
        const string sql = """
        SELECT "LegalRepresentativeId"
        FROM "Company"."LegalRepresentatives"
        WHERE "CompanyId" = @CompanyId
          AND "IsActive" = TRUE
        ORDER BY "CreatedDate" DESC
        LIMIT 1
        """;

        return await _connection.QuerySingleOrDefaultAsync<Guid?>(
            sql,
            new { CompanyId = companyId }
        );
    }

    private async Task<Unit> ExecuteUpdateLegalRepresentativeAsync(
        Guid legalRepresentativeId,
        string fullName,
        string documentType,
        string documentNumber,
        byte[]? nationalIdImage)
    {
        const string sql = """
        UPDATE "Company"."LegalRepresentatives"
        SET "FullName" = @FullName,
            "DocumentType" = @DocumentType,
            "DocumentNumber" = @DocumentNumber,
            "NationalIdImage" = @NationalIdImage,
            "UpdatedDate" = @UpdatedDate
        WHERE "LegalRepresentativeId" = @LegalRepresentativeId
        """;

        await _connection.ExecuteAsync(
            sql,
            new
            {
                LegalRepresentativeId = legalRepresentativeId,
                FullName = fullName,
                DocumentType = documentType,
                DocumentNumber = documentNumber,
                NationalIdImage = nationalIdImage,
                UpdatedDate = DateTime.UtcNow
            }
        );

        return Unit.Value;
    }

    private async Task<Guid> ExecuteInsertLegalRepresentativeAsync(
        Guid companyId,
        string fullName,
        string documentType,
        string documentNumber,
        byte[]? nationalIdImage)
    {
        const string sql = """
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

        var legalRepresentativeId = Guid.NewGuid();

        await _connection.ExecuteAsync(
            sql,
            new
            {
                LegalRepresentativeId = legalRepresentativeId,
                CompanyId = companyId,
                FullName = fullName,
                DocumentType = documentType,
                DocumentNumber = documentNumber,
                NationalIdImage = nationalIdImage,
                CreatedDate = DateTime.UtcNow
            }
        );

        return legalRepresentativeId;
    }

    private async Task<Guid?> ExecuteGetActiveBankAccountIdAsync(Guid companyId)
    {
        const string sql = """
        SELECT "BankAccountId"
        FROM "Company"."BankAccounts"
        WHERE "CompanyId" = @CompanyId
          AND "IsActive" = TRUE
        ORDER BY "CreatedDate" DESC
        LIMIT 1
        """;

        return await _connection.QuerySingleOrDefaultAsync<Guid?>(
            sql,
            new { CompanyId = companyId }
        );
    }

    private async Task<Unit> ExecuteUpdateBankAccountAsync(
        Guid bankAccountId,
        string bankName,
        string accountNumber,
        string cciCode)
    {
        const string sql = """
        UPDATE "Company"."BankAccounts"
        SET "BankName" = @BankName,
            "AccountNumber" = @AccountNumber,
            "CciCode" = @CciCode,
            "UpdatedDate" = @UpdatedDate
        WHERE "BankAccountId" = @BankAccountId
        """;

        await _connection.ExecuteAsync(
            sql,
            new
            {
                BankAccountId = bankAccountId,
                BankName = bankName,
                AccountNumber = accountNumber,
                CciCode = cciCode,
                UpdatedDate = DateTime.UtcNow
            }
        );

        return Unit.Value;
    }

    private async Task<Guid> ExecuteInsertBankAccountAsync(
        Guid companyId,
        string bankName,
        string accountNumber,
        string cciCode)
    {
        const string sql = """
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

        var bankAccountId = Guid.NewGuid();

        await _connection.ExecuteAsync(
            sql,
            new
            {
                BankAccountId = bankAccountId,
                CompanyId = companyId,
                BankName = bankName,
                AccountNumber = accountNumber,
                CciCode = cciCode,
                CreatedDate = DateTime.UtcNow
            }
        );

        return bankAccountId;
    }
}