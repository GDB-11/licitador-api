using Application.Core.DTOs.Company;
using Application.Core.Interfaces.Company;
using Application.Core.Services.Company;
using Global.Objects.Company;
using Global.Objects.Errors;
using Global.Objects.Functional;
using Global.Objects.Results;
using Infrastructure.Core.Interfaces.Account;
using Infrastructure.Core.Models.Company;
using NSubstitute;

namespace Application.Core.Services.Test.Company;

public sealed class CompanyServiceTests
{
    private readonly IUserRepository _userRepository;
    private readonly ICompany _sut;

    public CompanyServiceTests()
    {
        _userRepository = Substitute.For<IUserRepository>();
        _sut = new CompanyService(_userRepository);
    }

    #region GetUserCompanyAsync Tests

    [Fact]
    public async Task GetUserCompanyAsync_WithValidUserId_ReturnsSuccessWithUserCompanyResponse()
    {
        // Arrange
        var userId = Guid.NewGuid();
        var company = CreateTestCompany(
            companyId: Guid.NewGuid(),
            ruc: "20123456789",
            razonSocial: "Test Company S.A.C."
        );

        _userRepository.GetUserFirstCompanyAsync(userId)
            .Returns(Result<Infrastructure.Core.Models.Company.Company?, GenericError>.Success(company));

        // Act
        var result = await _sut.GetUserCompanyAsync(userId);

        // Assert
        Assert.True(result.IsSuccess);
        Assert.NotNull(result.Value);
        Assert.Equal(company.CompanyId, result.Value.CompanyId);
        Assert.Equal(company.Ruc, result.Value.Ruc);
        Assert.Equal(company.RazonSocial, result.Value.RazonSocial);

        await _userRepository.Received(1).GetUserFirstCompanyAsync(userId);
    }

    [Fact]
    public async Task GetUserCompanyAsync_WithNoCompanyFound_ReturnsCompanyNotFoundError()
    {
        // Arrange
        var userId = Guid.NewGuid();

        _userRepository.GetUserFirstCompanyAsync(userId)
            .Returns(Result<Infrastructure.Core.Models.Company.Company?, GenericError>.Success((Infrastructure.Core.Models.Company.Company?)null));

        // Act
        var result = await _sut.GetUserCompanyAsync(userId);

        // Assert
        Assert.False(result.IsSuccess);
        Assert.IsType<CompanyNotFoundError>(result.Error);
        Assert.Equal("No company found for the user", result.Error.Message);

        await _userRepository.Received(1).GetUserFirstCompanyAsync(userId);
    }

    [Fact]
    public async Task GetUserCompanyAsync_WhenRepositoryFails_ReturnsCompanyRepositoryError()
    {
        // Arrange
        var userId = Guid.NewGuid();
        var genericError = new GenericError("Database connection failed", "Connection timeout");

        _userRepository.GetUserFirstCompanyAsync(userId)
            .Returns(Result<Infrastructure.Core.Models.Company.Company?, GenericError>.Failure(genericError));

        // Act
        var result = await _sut.GetUserCompanyAsync(userId);

        // Assert
        Assert.False(result.IsSuccess);
        Assert.IsType<CompanyRepositoryError>(result.Error);
        Assert.Equal("An error occurred while retrieving company information", result.Error.Message);
        Assert.Equal("Database connection failed", result.Error.Details);

        await _userRepository.Received(1).GetUserFirstCompanyAsync(userId);
    }

    [Fact]
    public async Task GetUserCompanyAsync_WhenRepositoryFailsWithException_ReturnsCompanyRepositoryErrorWithException()
    {
        // Arrange
        var userId = Guid.NewGuid();
        var exception = new InvalidOperationException("Connection lost");
        var genericError = new GenericError("Database error", "Failed to query", exception);

        _userRepository.GetUserFirstCompanyAsync(userId)
            .Returns(Result<Infrastructure.Core.Models.Company.Company?, GenericError>.Failure(genericError));

        // Act
        var result = await _sut.GetUserCompanyAsync(userId);

        // Assert
        Assert.False(result.IsSuccess);
        Assert.IsType<CompanyRepositoryError>(result.Error);
        Assert.Equal("An error occurred while retrieving company information", result.Error.Message);
        Assert.Equal("Database error", result.Error.Details);
        Assert.Same(exception, result.Error.Exception);

        await _userRepository.Received(1).GetUserFirstCompanyAsync(userId);
    }

    [Fact]
    public async Task GetUserCompanyAsync_WithDifferentUserIds_CallsRepositoryWithCorrectUserId()
    {
        // Arrange
        var userId1 = Guid.NewGuid();
        var userId2 = Guid.NewGuid();
        var company = CreateTestCompany();

        _userRepository.GetUserFirstCompanyAsync(Arg.Any<Guid>())
            .Returns(Result<Infrastructure.Core.Models.Company.Company?, GenericError>.Success(company));

        // Act
        await _sut.GetUserCompanyAsync(userId1);
        await _sut.GetUserCompanyAsync(userId2);

        // Assert
        await _userRepository.Received(1).GetUserFirstCompanyAsync(userId1);
        await _userRepository.Received(1).GetUserFirstCompanyAsync(userId2);
        await _userRepository.Received(2).GetUserFirstCompanyAsync(Arg.Any<Guid>());
    }

    [Fact]
    public async Task GetUserCompanyAsync_MapsAllCompanyFields_ReturnsCompleteUserCompanyResponse()
    {
        // Arrange
        var userId = Guid.NewGuid();
        var companyId = Guid.NewGuid();
        var ruc = "20987654321";
        var razonSocial = "Empresa Test S.R.L.";

        var company = CreateTestCompany(
            companyId: companyId,
            ruc: ruc,
            razonSocial: razonSocial
        );

        _userRepository.GetUserFirstCompanyAsync(userId)
            .Returns(Result<Infrastructure.Core.Models.Company.Company?, GenericError>.Success(company));

        // Act
        var result = await _sut.GetUserCompanyAsync(userId);

        // Assert
        Assert.True(result.IsSuccess);
        Assert.Equal(companyId, result.Value.CompanyId);
        Assert.Equal(ruc, result.Value.Ruc);
        Assert.Equal(razonSocial, result.Value.RazonSocial);
    }

    [Fact]
    public async Task GetUserCompanyAsync_WithEmptyGuid_StillCallsRepository()
    {
        // Arrange
        var userId = Guid.Empty;
        var company = CreateTestCompany();

        _userRepository.GetUserFirstCompanyAsync(userId)
            .Returns(Result<Infrastructure.Core.Models.Company.Company?, GenericError>.Success(company));

        // Act
        var result = await _sut.GetUserCompanyAsync(userId);

        // Assert
        Assert.True(result.IsSuccess);
        await _userRepository.Received(1).GetUserFirstCompanyAsync(userId);
    }

    [Fact]
    public async Task GetUserCompanyAsync_WithMultipleCallsSameUser_CallsRepositoryEachTime()
    {
        // Arrange
        var userId = Guid.NewGuid();
        var company = CreateTestCompany();

        _userRepository.GetUserFirstCompanyAsync(userId)
            .Returns(Result<Infrastructure.Core.Models.Company.Company?, GenericError>.Success(company));

        // Act
        await _sut.GetUserCompanyAsync(userId);
        await _sut.GetUserCompanyAsync(userId);
        await _sut.GetUserCompanyAsync(userId);

        // Assert
        await _userRepository.Received(3).GetUserFirstCompanyAsync(userId);
    }

    [Fact]
    public async Task GetUserCompanyAsync_WithSpecialCharactersInCompanyData_ReturnsCorrectData()
    {
        // Arrange
        var userId = Guid.NewGuid();
        var company = CreateTestCompany(
            ruc: "20123456789",
            razonSocial: "Test & Company S.A. - Sucursal Lima"
        );

        _userRepository.GetUserFirstCompanyAsync(userId)
            .Returns(Result<Infrastructure.Core.Models.Company.Company?, GenericError>.Success(company));

        // Act
        var result = await _sut.GetUserCompanyAsync(userId);

        // Assert
        Assert.True(result.IsSuccess);
        Assert.Equal("Test & Company S.A. - Sucursal Lima", result.Value.RazonSocial);
    }

    [Fact]
    public async Task GetUserCompanyAsync_PreservesErrorDetails_WhenRepositoryFails()
    {
        // Arrange
        var userId = Guid.NewGuid();
        var errorMessage = "Specific database error";
        var errorDetails = "Table 'Companies' does not exist";
        var genericError = new GenericError(errorMessage, errorDetails);

        _userRepository.GetUserFirstCompanyAsync(userId)
            .Returns(Result<Infrastructure.Core.Models.Company.Company?, GenericError>.Failure(genericError));

        // Act
        var result = await _sut.GetUserCompanyAsync(userId);

        // Assert
        Assert.False(result.IsSuccess);
        var companyError = Assert.IsType<CompanyRepositoryError>(result.Error);
        Assert.Equal(errorMessage, companyError.Details);
    }

    #endregion

    #region GetUserCompanyDetailsAsync Tests

    [Fact]
    public async Task GetUserCompanyDetailsAsync_WithValidOwnership_ReturnsCompanyDetails()
    {
        // Arrange
        var userId = Guid.NewGuid();
        var companyId = Guid.NewGuid();
        var companyDetails = CreateTestCompanyDetails(companyId);

        _userRepository.ValidateUserCompanyOwnershipAsync(userId, companyId)
            .Returns(Result<bool, GenericError>.Success(true));
        _userRepository.GetCompanyDetailsAsync(companyId)
            .Returns(Result<CompanyDetails?, GenericError>.Success(companyDetails));

        // Act
        var result = await _sut.GetUserCompanyDetailsAsync(userId, companyId);

        // Assert
        Assert.True(result.IsSuccess);
        Assert.NotNull(result.Value);
        Assert.Equal(companyId, result.Value.CompanyId);
        Assert.Equal("20123456789", result.Value.Ruc);
        Assert.Equal("Test Company S.A.C.", result.Value.RazonSocial);

        await _userRepository.Received(1).ValidateUserCompanyOwnershipAsync(userId, companyId);
        await _userRepository.Received(1).GetCompanyDetailsAsync(companyId);
    }

    [Fact]
    public async Task GetUserCompanyDetailsAsync_WithNoOwnership_ReturnsUnauthorizedAccessError()
    {
        // Arrange
        var userId = Guid.NewGuid();
        var companyId = Guid.NewGuid();

        _userRepository.ValidateUserCompanyOwnershipAsync(userId, companyId)
            .Returns(Result<bool, GenericError>.Success(false));

        // Act
        var result = await _sut.GetUserCompanyDetailsAsync(userId, companyId);

        // Assert
        Assert.False(result.IsSuccess);
        Assert.IsType<CompanyUnauthorizedAccessError>(result.Error);
        Assert.Equal("User does not have access to the specified company", result.Error.Message);

        await _userRepository.Received(1).ValidateUserCompanyOwnershipAsync(userId, companyId);
        await _userRepository.DidNotReceive().GetCompanyDetailsAsync(Arg.Any<Guid>());
    }

    [Fact]
    public async Task GetUserCompanyDetailsAsync_WhenOwnershipValidationFails_ReturnsRepositoryError()
    {
        // Arrange
        var userId = Guid.NewGuid();
        var companyId = Guid.NewGuid();
        var genericError = new GenericError("Database error", "Connection lost");

        _userRepository.ValidateUserCompanyOwnershipAsync(userId, companyId)
            .Returns(Result<bool, GenericError>.Failure(genericError));

        // Act
        var result = await _sut.GetUserCompanyDetailsAsync(userId, companyId);

        // Assert
        Assert.False(result.IsSuccess);
        Assert.IsType<CompanyRepositoryError>(result.Error);
        Assert.Equal("Database error", result.Error.Details);

        await _userRepository.Received(1).ValidateUserCompanyOwnershipAsync(userId, companyId);
        await _userRepository.DidNotReceive().GetCompanyDetailsAsync(Arg.Any<Guid>());
    }

    [Fact]
    public async Task GetUserCompanyDetailsAsync_WhenCompanyNotFound_ReturnsCompanyNotFoundError()
    {
        // Arrange
        var userId = Guid.NewGuid();
        var companyId = Guid.NewGuid();

        _userRepository.ValidateUserCompanyOwnershipAsync(userId, companyId)
            .Returns(Result<bool, GenericError>.Success(true));
        _userRepository.GetCompanyDetailsAsync(companyId)
            .Returns(Result<CompanyDetails?, GenericError>.Success((CompanyDetails?)null));

        // Act
        var result = await _sut.GetUserCompanyDetailsAsync(userId, companyId);

        // Assert
        Assert.False(result.IsSuccess);
        Assert.IsType<CompanyNotFoundError>(result.Error);
    }

    [Fact]
    public async Task GetUserCompanyDetailsAsync_WithLegalRepresentative_MapsCorrectly()
    {
        // Arrange
        var userId = Guid.NewGuid();
        var companyId = Guid.NewGuid();
        var legalRepId = Guid.NewGuid();
        var companyDetails = CreateTestCompanyDetails(companyId, legalRepresentativeId: legalRepId);

        _userRepository.ValidateUserCompanyOwnershipAsync(userId, companyId)
            .Returns(Result<bool, GenericError>.Success(true));
        _userRepository.GetCompanyDetailsAsync(companyId)
            .Returns(Result<CompanyDetails?, GenericError>.Success(companyDetails));

        // Act
        var result = await _sut.GetUserCompanyDetailsAsync(userId, companyId);

        // Assert
        Assert.True(result.IsSuccess);
        Assert.NotNull(result.Value.LegalRepresentative);
        Assert.Equal(legalRepId, result.Value.LegalRepresentative.LegalRepresentativeId);
        Assert.Equal("John Doe", result.Value.LegalRepresentative.FullName);
        Assert.Equal("DNI", result.Value.LegalRepresentative.DocumentType);
        Assert.Equal("12345678", result.Value.LegalRepresentative.DocumentNumber);
    }

    [Fact]
    public async Task GetUserCompanyDetailsAsync_WithBankAccount_MapsCorrectly()
    {
        // Arrange
        var userId = Guid.NewGuid();
        var companyId = Guid.NewGuid();
        var bankAccountId = Guid.NewGuid();
        var companyDetails = CreateTestCompanyDetails(companyId, bankAccountId: bankAccountId);

        _userRepository.ValidateUserCompanyOwnershipAsync(userId, companyId)
            .Returns(Result<bool, GenericError>.Success(true));
        _userRepository.GetCompanyDetailsAsync(companyId)
            .Returns(Result<CompanyDetails?, GenericError>.Success(companyDetails));

        // Act
        var result = await _sut.GetUserCompanyDetailsAsync(userId, companyId);

        // Assert
        Assert.True(result.IsSuccess);
        Assert.NotNull(result.Value.BankAccount);
        Assert.Equal(bankAccountId, result.Value.BankAccount.BankAccountId);
        Assert.Equal("BCP", result.Value.BankAccount.BankName);
        Assert.Equal("1234567890", result.Value.BankAccount.AccountNumber);
        Assert.Equal("12345678901234567890", result.Value.BankAccount.CciCode);
    }

    [Fact]
    public async Task GetUserCompanyDetailsAsync_WithNationalIdImage_ConvertsToBase64()
    {
        // Arrange
        var userId = Guid.NewGuid();
        var companyId = Guid.NewGuid();
        var imageBytes = new byte[] { 1, 2, 3, 4, 5 };
        var companyDetails = CreateTestCompanyDetails(companyId, legalRepresentativeId: Guid.NewGuid(), nationalIdImage: imageBytes);

        _userRepository.ValidateUserCompanyOwnershipAsync(userId, companyId)
            .Returns(Result<bool, GenericError>.Success(true));
        _userRepository.GetCompanyDetailsAsync(companyId)
            .Returns(Result<CompanyDetails?, GenericError>.Success(companyDetails));

        // Act
        var result = await _sut.GetUserCompanyDetailsAsync(userId, companyId);

        // Assert
        Assert.True(result.IsSuccess);
        Assert.NotNull(result.Value.LegalRepresentative);
        Assert.Equal(Convert.ToBase64String(imageBytes), result.Value.LegalRepresentative.NationalIdImage);
    }

    [Fact]
    public async Task GetUserCompanyDetailsAsync_WithNullNationalIdImage_ReturnsNull()
    {
        // Arrange
        var userId = Guid.NewGuid();
        var companyId = Guid.NewGuid();
        var companyDetails = CreateTestCompanyDetails(companyId, legalRepresentativeId: Guid.NewGuid(), nationalIdImage: null);

        _userRepository.ValidateUserCompanyOwnershipAsync(userId, companyId)
            .Returns(Result<bool, GenericError>.Success(true));
        _userRepository.GetCompanyDetailsAsync(companyId)
            .Returns(Result<CompanyDetails?, GenericError>.Success(companyDetails));

        // Act
        var result = await _sut.GetUserCompanyDetailsAsync(userId, companyId);

        // Assert
        Assert.True(result.IsSuccess);
        Assert.NotNull(result.Value.LegalRepresentative);
        Assert.Null(result.Value.LegalRepresentative.NationalIdImage);
    }

    [Fact]
    public async Task GetUserCompanyDetailsAsync_WithoutLegalRepresentative_ReturnsNullLegalRep()
    {
        // Arrange
        var userId = Guid.NewGuid();
        var companyId = Guid.NewGuid();
        var companyDetails = CreateTestCompanyDetails(companyId, legalRepresentativeId: null);

        _userRepository.ValidateUserCompanyOwnershipAsync(userId, companyId)
            .Returns(Result<bool, GenericError>.Success(true));
        _userRepository.GetCompanyDetailsAsync(companyId)
            .Returns(Result<CompanyDetails?, GenericError>.Success(companyDetails));

        // Act
        var result = await _sut.GetUserCompanyDetailsAsync(userId, companyId);

        // Assert
        Assert.True(result.IsSuccess);
        Assert.Null(result.Value.LegalRepresentative);
    }

    [Fact]
    public async Task GetUserCompanyDetailsAsync_WithoutBankAccount_ReturnsNullBankAccount()
    {
        // Arrange
        var userId = Guid.NewGuid();
        var companyId = Guid.NewGuid();
        var companyDetails = CreateTestCompanyDetails(companyId, bankAccountId: null);

        _userRepository.ValidateUserCompanyOwnershipAsync(userId, companyId)
            .Returns(Result<bool, GenericError>.Success(true));
        _userRepository.GetCompanyDetailsAsync(companyId)
            .Returns(Result<CompanyDetails?, GenericError>.Success(companyDetails));

        // Act
        var result = await _sut.GetUserCompanyDetailsAsync(userId, companyId);

        // Assert
        Assert.True(result.IsSuccess);
        Assert.Null(result.Value.BankAccount);
    }

    [Fact]
    public async Task GetUserCompanyDetailsAsync_WhenGetDetailsRepositoryFails_ReturnsRepositoryError()
    {
        // Arrange
        var userId = Guid.NewGuid();
        var companyId = Guid.NewGuid();
        var genericError = new GenericError("Query failed", "Database timeout");

        _userRepository.ValidateUserCompanyOwnershipAsync(userId, companyId)
            .Returns(Result<bool, GenericError>.Success(true));
        _userRepository.GetCompanyDetailsAsync(companyId)
            .Returns(Result<CompanyDetails?, GenericError>.Failure(genericError));

        // Act
        var result = await _sut.GetUserCompanyDetailsAsync(userId, companyId);

        // Assert
        Assert.False(result.IsSuccess);
        Assert.IsType<CompanyRepositoryError>(result.Error);
        Assert.Equal("Query failed", result.Error.Details);
    }

    #endregion

    #region UpdateCompanyDetailsAsync Tests

    [Fact]
    public async Task UpdateCompanyDetailsAsync_WithValidRequest_ReturnsSuccess()
    {
        // Arrange
        var userId = Guid.NewGuid();
        var companyId = Guid.NewGuid();
        var request = CreateUpdateRequest(companyId);

        _userRepository.ValidateUserCompanyOwnershipAsync(userId, companyId)
            .Returns(Result<bool, GenericError>.Success(true));
        _userRepository.UpdateCompanyAsync(
            Arg.Any<Guid>(),
            Arg.Any<string>(),
            Arg.Any<string>(),
            Arg.Any<string>(),
            Arg.Any<string?>(),
            Arg.Any<string>(),
            Arg.Any<DateTime?>(),
            Arg.Any<bool>())
            .Returns(Result<Unit, GenericError>.Success(Unit.Value));

        // Act
        var result = await _sut.UpdateCompanyDetailsAsync(userId, request);

        // Assert
        Assert.True(result.IsSuccess);
        await _userRepository.Received(1).ValidateUserCompanyOwnershipAsync(userId, companyId);
        await _userRepository.Received(1).UpdateCompanyAsync(
            companyId,
            request.Ruc,
            request.RazonSocial,
            request.DomicilioLegal,
            request.Telefono,
            request.Email,
            request.FechaConstitucion,
            request.IsMype);
    }

    [Fact]
    public async Task UpdateCompanyDetailsAsync_WithNoOwnership_ReturnsUnauthorizedError()
    {
        // Arrange
        var userId = Guid.NewGuid();
        var companyId = Guid.NewGuid();
        var request = CreateUpdateRequest(companyId);

        _userRepository.ValidateUserCompanyOwnershipAsync(userId, companyId)
            .Returns(Result<bool, GenericError>.Success(false));

        // Act
        var result = await _sut.UpdateCompanyDetailsAsync(userId, request);

        // Assert
        Assert.False(result.IsSuccess);
        Assert.IsType<CompanyUnauthorizedAccessError>(result.Error);
        await _userRepository.DidNotReceive().UpdateCompanyAsync(
            Arg.Any<Guid>(),
            Arg.Any<string>(),
            Arg.Any<string>(),
            Arg.Any<string>(),
            Arg.Any<string?>(),
            Arg.Any<string>(),
            Arg.Any<DateTime?>(),
            Arg.Any<bool>());
    }

    [Fact]
    public async Task UpdateCompanyDetailsAsync_WithInvalidBase64Image_ReturnsValidationError()
    {
        // Arrange
        var userId = Guid.CreateVersion7();
        var companyId = Guid.CreateVersion7();

        var request = new UpdateCompanyDetailsRequest
        {
            CompanyId = companyId,
            Ruc = "20123456789",
            RazonSocial = "Updated Company S.A.C.",
            DomicilioLegal = "Av. Updated 456, Lima",
            Telefono = "999888777",
            Email = "updated@company.com",
            FechaConstitucion = new DateTime(2020, 1, 1),
            IsMype = true,
            LegalRepresentative = new UpdateLegalRepresentativeRequest
            {
                FullName = "Jane Smith",
                DocumentType = "DNI",
                DocumentNumber = "87654321",
                NationalIdImage = "!!!NOT-VALID-BASE64!!!" // This will definitely fail
            },
            BankAccount = null
        };

        // Act
        var result = await _sut.UpdateCompanyDetailsAsync(userId, request);

        // Assert
        Assert.False(result.IsSuccess);
        Assert.IsType<CompanyValidationError>(result.Error);
        Assert.Contains("Invalid Base64 format", result.Error.Details);
        await _userRepository.DidNotReceive().ValidateUserCompanyOwnershipAsync(Arg.Any<Guid>(), Arg.Any<Guid>());
    }

    [Fact]
    public async Task UpdateCompanyDetailsAsync_WithLegalRepresentativeUpdate_UpdatesExisting()
    {
        // Arrange
        var userId = Guid.NewGuid();
        var companyId = Guid.NewGuid();
        var existingLegalRepId = Guid.NewGuid();
        var request = CreateUpdateRequest(companyId, includeLegalRep: true);

        _userRepository.ValidateUserCompanyOwnershipAsync(userId, companyId)
            .Returns(Result<bool, GenericError>.Success(true));
        _userRepository.UpdateCompanyAsync(Arg.Any<Guid>(), Arg.Any<string>(), Arg.Any<string>(), Arg.Any<string>(), Arg.Any<string?>(), Arg.Any<string>(), Arg.Any<DateTime?>(), Arg.Any<bool>())
            .Returns(Result<Unit, GenericError>.Success(Unit.Value));
        _userRepository.GetActiveLegalRepresentativeIdAsync(companyId)
            .Returns(Result<Guid?, GenericError>.Success(existingLegalRepId));
        _userRepository.UpdateLegalRepresentativeAsync(Arg.Any<Guid>(), Arg.Any<string>(), Arg.Any<string>(), Arg.Any<string>(), Arg.Any<byte[]?>())
            .Returns(Result<Unit, GenericError>.Success(Unit.Value));

        // Act
        var result = await _sut.UpdateCompanyDetailsAsync(userId, request);

        // Assert
        Assert.True(result.IsSuccess);
        await _userRepository.Received(1).GetActiveLegalRepresentativeIdAsync(companyId);
        await _userRepository.Received(1).UpdateLegalRepresentativeAsync(
            existingLegalRepId,
            "Jane Smith",
            Arg.Any<string>(),
            Arg.Any<string>(),
            Arg.Any<byte[]?>());
        await _userRepository.DidNotReceive().InsertLegalRepresentativeAsync(Arg.Any<Guid>(), Arg.Any<string>(), Arg.Any<string>(), Arg.Any<string>(), Arg.Any<byte[]?>());
    }

    [Fact]
    public async Task UpdateCompanyDetailsAsync_WithLegalRepresentativeInsert_InsertsNew()
    {
        // Arrange
        var userId = Guid.NewGuid();
        var companyId = Guid.NewGuid();
        var request = CreateUpdateRequest(companyId, includeLegalRep: true);

        _userRepository.ValidateUserCompanyOwnershipAsync(userId, companyId)
            .Returns(Result<bool, GenericError>.Success(true));
        _userRepository.UpdateCompanyAsync(Arg.Any<Guid>(), Arg.Any<string>(), Arg.Any<string>(), Arg.Any<string>(), Arg.Any<string?>(), Arg.Any<string>(), Arg.Any<DateTime?>(), Arg.Any<bool>())
            .Returns(Result<Unit, GenericError>.Success(Unit.Value));
        _userRepository.GetActiveLegalRepresentativeIdAsync(companyId)
            .Returns(Result<Guid?, GenericError>.Success((Guid?)null));
        _userRepository.InsertLegalRepresentativeAsync(Arg.Any<Guid>(), Arg.Any<string>(), Arg.Any<string>(), Arg.Any<string>(), Arg.Any<byte[]?>())
            .Returns(Result<Guid, GenericError>.Success(Guid.NewGuid()));

        // Act
        var result = await _sut.UpdateCompanyDetailsAsync(userId, request);

        // Assert
        Assert.True(result.IsSuccess);
        await _userRepository.Received(1).GetActiveLegalRepresentativeIdAsync(companyId);
        await _userRepository.Received(1).InsertLegalRepresentativeAsync(
            companyId,
            "Jane Smith",
            Arg.Any<string>(),
            Arg.Any<string>(),
            Arg.Any<byte[]?>());
        await _userRepository.DidNotReceive().UpdateLegalRepresentativeAsync(Arg.Any<Guid>(), Arg.Any<string>(), Arg.Any<string>(), Arg.Any<string>(), Arg.Any<byte[]?>());
    }

    [Fact]
    public async Task UpdateCompanyDetailsAsync_WithBankAccountUpdate_UpdatesExisting()
    {
        // Arrange
        var userId = Guid.NewGuid();
        var companyId = Guid.NewGuid();
        var existingBankAccountId = Guid.NewGuid();
        var request = CreateUpdateRequest(companyId, includeBankAccount: true);

        _userRepository.ValidateUserCompanyOwnershipAsync(userId, companyId)
            .Returns(Result<bool, GenericError>.Success(true));
        _userRepository.UpdateCompanyAsync(Arg.Any<Guid>(), Arg.Any<string>(), Arg.Any<string>(), Arg.Any<string>(), Arg.Any<string?>(), Arg.Any<string>(), Arg.Any<DateTime?>(), Arg.Any<bool>())
            .Returns(Result<Unit, GenericError>.Success(Unit.Value));
        _userRepository.GetActiveBankAccountIdAsync(companyId)
            .Returns(Result<Guid?, GenericError>.Success(existingBankAccountId));
        _userRepository.UpdateBankAccountAsync(Arg.Any<Guid>(), Arg.Any<string>(), Arg.Any<string>(), Arg.Any<string>())
            .Returns(Result<Unit, GenericError>.Success(Unit.Value));

        // Act
        var result = await _sut.UpdateCompanyDetailsAsync(userId, request);

        // Assert
        Assert.True(result.IsSuccess);
        await _userRepository.Received(1).GetActiveBankAccountIdAsync(companyId);
        await _userRepository.Received(1).UpdateBankAccountAsync(
            existingBankAccountId,
            "Interbank",
            Arg.Any<string>(),
            Arg.Any<string>());
        await _userRepository.DidNotReceive().InsertBankAccountAsync(Arg.Any<Guid>(), Arg.Any<string>(), Arg.Any<string>(), Arg.Any<string>());
    }

    [Fact]
    public async Task UpdateCompanyDetailsAsync_WithBankAccountInsert_InsertsNew()
    {
        // Arrange
        var userId = Guid.NewGuid();
        var companyId = Guid.NewGuid();
        var request = CreateUpdateRequest(companyId, includeBankAccount: true);

        _userRepository.ValidateUserCompanyOwnershipAsync(userId, companyId)
            .Returns(Result<bool, GenericError>.Success(true));
        _userRepository.UpdateCompanyAsync(Arg.Any<Guid>(), Arg.Any<string>(), Arg.Any<string>(), Arg.Any<string>(), Arg.Any<string?>(), Arg.Any<string>(), Arg.Any<DateTime?>(), Arg.Any<bool>())
            .Returns(Result<Unit, GenericError>.Success(Unit.Value));
        _userRepository.GetActiveBankAccountIdAsync(companyId)
            .Returns(Result<Guid?, GenericError>.Success((Guid?)null));
        _userRepository.InsertBankAccountAsync(Arg.Any<Guid>(), Arg.Any<string>(), Arg.Any<string>(), Arg.Any<string>())
            .Returns(Result<Guid, GenericError>.Success(Guid.NewGuid()));

        // Act
        var result = await _sut.UpdateCompanyDetailsAsync(userId, request);

        // Assert
        Assert.True(result.IsSuccess);
        await _userRepository.Received(1).GetActiveBankAccountIdAsync(companyId);
        await _userRepository.Received(1).InsertBankAccountAsync(
            companyId,
            "Interbank",
            Arg.Any<string>(),
            Arg.Any<string>());
        await _userRepository.DidNotReceive().UpdateBankAccountAsync(Arg.Any<Guid>(), Arg.Any<string>(), Arg.Any<string>(), Arg.Any<string>());
    }

    [Fact]
    public async Task UpdateCompanyDetailsAsync_WithoutLegalRepOrBankAccount_OnlyUpdatesCompany()
    {
        // Arrange
        var userId = Guid.NewGuid();
        var companyId = Guid.NewGuid();
        var request = CreateUpdateRequest(companyId);

        _userRepository.ValidateUserCompanyOwnershipAsync(userId, companyId)
            .Returns(Result<bool, GenericError>.Success(true));
        _userRepository.UpdateCompanyAsync(Arg.Any<Guid>(), Arg.Any<string>(), Arg.Any<string>(), Arg.Any<string>(), Arg.Any<string?>(), Arg.Any<string>(), Arg.Any<DateTime?>(), Arg.Any<bool>())
            .Returns(Result<Unit, GenericError>.Success(Unit.Value));

        // Act
        var result = await _sut.UpdateCompanyDetailsAsync(userId, request);

        // Assert
        Assert.True(result.IsSuccess);
        await _userRepository.Received(1).UpdateCompanyAsync(Arg.Any<Guid>(), Arg.Any<string>(), Arg.Any<string>(), Arg.Any<string>(), Arg.Any<string?>(), Arg.Any<string>(), Arg.Any<DateTime?>(), Arg.Any<bool>());
        await _userRepository.DidNotReceive().GetActiveLegalRepresentativeIdAsync(Arg.Any<Guid>());
        await _userRepository.DidNotReceive().GetActiveBankAccountIdAsync(Arg.Any<Guid>());
    }

    [Fact]
    public async Task UpdateCompanyDetailsAsync_WhenUpdateCompanyFails_ReturnsRepositoryError()
    {
        // Arrange
        var userId = Guid.NewGuid();
        var companyId = Guid.NewGuid();
        var request = CreateUpdateRequest(companyId);
        var genericError = new GenericError("Update failed", "Database error");

        _userRepository.ValidateUserCompanyOwnershipAsync(userId, companyId)
            .Returns(Result<bool, GenericError>.Success(true));
        _userRepository.UpdateCompanyAsync(Arg.Any<Guid>(), Arg.Any<string>(), Arg.Any<string>(), Arg.Any<string>(), Arg.Any<string?>(), Arg.Any<string>(), Arg.Any<DateTime?>(), Arg.Any<bool>())
            .Returns(Result<Unit, GenericError>.Failure(genericError));

        // Act
        var result = await _sut.UpdateCompanyDetailsAsync(userId, request);

        // Assert
        Assert.False(result.IsSuccess);
        Assert.IsType<CompanyRepositoryError>(result.Error);
        Assert.Equal("Update failed", result.Error.Details);
    }

    [Fact]
    public async Task UpdateCompanyDetailsAsync_WhenGetLegalRepIdFails_ReturnsRepositoryError()
    {
        // Arrange
        var userId = Guid.NewGuid();
        var companyId = Guid.NewGuid();
        var request = CreateUpdateRequest(companyId, includeLegalRep: true);
        var genericError = new GenericError("Query failed", "Connection lost");

        _userRepository.ValidateUserCompanyOwnershipAsync(userId, companyId)
            .Returns(Result<bool, GenericError>.Success(true));
        _userRepository.UpdateCompanyAsync(Arg.Any<Guid>(), Arg.Any<string>(), Arg.Any<string>(), Arg.Any<string>(), Arg.Any<string?>(), Arg.Any<string>(), Arg.Any<DateTime?>(), Arg.Any<bool>())
            .Returns(Result<Unit, GenericError>.Success(Unit.Value));
        _userRepository.GetActiveLegalRepresentativeIdAsync(companyId)
            .Returns(Result<Guid?, GenericError>.Failure(genericError));

        // Act
        var result = await _sut.UpdateCompanyDetailsAsync(userId, request);

        // Assert
        Assert.False(result.IsSuccess);
        Assert.IsType<CompanyRepositoryError>(result.Error);
        Assert.Equal("Query failed", result.Error.Details);
    }

    [Fact]
    public async Task UpdateCompanyDetailsAsync_WhenUpdateLegalRepFails_ReturnsRepositoryError()
    {
        // Arrange
        var userId = Guid.NewGuid();
        var companyId = Guid.NewGuid();
        var existingLegalRepId = Guid.NewGuid();
        var request = CreateUpdateRequest(companyId, includeLegalRep: true);
        var genericError = new GenericError("Update failed", "Constraint violation");

        _userRepository.ValidateUserCompanyOwnershipAsync(userId, companyId)
            .Returns(Result<bool, GenericError>.Success(true));
        _userRepository.UpdateCompanyAsync(Arg.Any<Guid>(), Arg.Any<string>(), Arg.Any<string>(), Arg.Any<string>(), Arg.Any<string?>(), Arg.Any<string>(), Arg.Any<DateTime?>(), Arg.Any<bool>())
            .Returns(Result<Unit, GenericError>.Success(Unit.Value));
        _userRepository.GetActiveLegalRepresentativeIdAsync(companyId)
            .Returns(Result<Guid?, GenericError>.Success(existingLegalRepId));
        _userRepository.UpdateLegalRepresentativeAsync(Arg.Any<Guid>(), Arg.Any<string>(), Arg.Any<string>(), Arg.Any<string>(), Arg.Any<byte[]?>())
            .Returns(Result<Unit, GenericError>.Failure(genericError));

        // Act
        var result = await _sut.UpdateCompanyDetailsAsync(userId, request);

        // Assert
        Assert.False(result.IsSuccess);
        Assert.IsType<CompanyRepositoryError>(result.Error);
        Assert.Equal("Update failed", result.Error.Details);
    }

    [Fact]
    public async Task UpdateCompanyDetailsAsync_WhenInsertLegalRepFails_ReturnsRepositoryError()
    {
        // Arrange
        var userId = Guid.NewGuid();
        var companyId = Guid.NewGuid();
        var request = CreateUpdateRequest(companyId, includeLegalRep: true);
        var genericError = new GenericError("Insert failed", "Foreign key violation");

        _userRepository.ValidateUserCompanyOwnershipAsync(userId, companyId)
            .Returns(Result<bool, GenericError>.Success(true));
        _userRepository.UpdateCompanyAsync(Arg.Any<Guid>(), Arg.Any<string>(), Arg.Any<string>(), Arg.Any<string>(), Arg.Any<string?>(), Arg.Any<string>(), Arg.Any<DateTime?>(), Arg.Any<bool>())
            .Returns(Result<Unit, GenericError>.Success(Unit.Value));
        _userRepository.GetActiveLegalRepresentativeIdAsync(companyId)
            .Returns(Result<Guid?, GenericError>.Success((Guid?)null));
        _userRepository.InsertLegalRepresentativeAsync(Arg.Any<Guid>(), Arg.Any<string>(), Arg.Any<string>(), Arg.Any<string>(), Arg.Any<byte[]?>())
            .Returns(Result<Guid, GenericError>.Failure(genericError));

        // Act
        var result = await _sut.UpdateCompanyDetailsAsync(userId, request);

        // Assert
        Assert.False(result.IsSuccess);
        Assert.IsType<CompanyRepositoryError>(result.Error);
        Assert.Equal("Insert failed", result.Error.Details);
    }

    [Fact]
    public async Task UpdateCompanyDetailsAsync_WhenUpdateBankAccountFails_ReturnsRepositoryError()
    {
        // Arrange
        var userId = Guid.NewGuid();
        var companyId = Guid.NewGuid();
        var existingBankAccountId = Guid.NewGuid();
        var request = CreateUpdateRequest(companyId, includeBankAccount: true);
        var genericError = new GenericError("Update failed", "Validation error");

        _userRepository.ValidateUserCompanyOwnershipAsync(userId, companyId)
            .Returns(Result<bool, GenericError>.Success(true));
        _userRepository.UpdateCompanyAsync(Arg.Any<Guid>(), Arg.Any<string>(), Arg.Any<string>(), Arg.Any<string>(), Arg.Any<string?>(), Arg.Any<string>(), Arg.Any<DateTime?>(), Arg.Any<bool>())
            .Returns(Result<Unit, GenericError>.Success(Unit.Value));
        _userRepository.GetActiveBankAccountIdAsync(companyId)
            .Returns(Result<Guid?, GenericError>.Success(existingBankAccountId));
        _userRepository.UpdateBankAccountAsync(Arg.Any<Guid>(), Arg.Any<string>(), Arg.Any<string>(), Arg.Any<string>())
            .Returns(Result<Unit, GenericError>.Failure(genericError));

        // Act
        var result = await _sut.UpdateCompanyDetailsAsync(userId, request);

        // Assert
        Assert.False(result.IsSuccess);
        Assert.IsType<CompanyRepositoryError>(result.Error);
        Assert.Equal("Update failed", result.Error.Details);
    }

    [Fact]
    public async Task UpdateCompanyDetailsAsync_WithBothLegalRepAndBankAccount_UpdatesBoth()
    {
        // Arrange
        var userId = Guid.NewGuid();
        var companyId = Guid.NewGuid();
        var request = CreateUpdateRequest(companyId, includeLegalRep: true, includeBankAccount: true);

        _userRepository.ValidateUserCompanyOwnershipAsync(userId, companyId)
            .Returns(Result<bool, GenericError>.Success(true));
        _userRepository.UpdateCompanyAsync(Arg.Any<Guid>(), Arg.Any<string>(), Arg.Any<string>(), Arg.Any<string>(), Arg.Any<string?>(), Arg.Any<string>(), Arg.Any<DateTime?>(), Arg.Any<bool>())
            .Returns(Result<Unit, GenericError>.Success(Unit.Value));
        _userRepository.GetActiveLegalRepresentativeIdAsync(companyId)
            .Returns(Result<Guid?, GenericError>.Success((Guid?)null));
        _userRepository.InsertLegalRepresentativeAsync(Arg.Any<Guid>(), Arg.Any<string>(), Arg.Any<string>(), Arg.Any<string>(), Arg.Any<byte[]?>())
            .Returns(Result<Guid, GenericError>.Success(Guid.NewGuid()));
        _userRepository.GetActiveBankAccountIdAsync(companyId)
            .Returns(Result<Guid?, GenericError>.Success((Guid?)null));
        _userRepository.InsertBankAccountAsync(Arg.Any<Guid>(), Arg.Any<string>(), Arg.Any<string>(), Arg.Any<string>())
            .Returns(Result<Guid, GenericError>.Success(Guid.NewGuid()));

        // Act
        var result = await _sut.UpdateCompanyDetailsAsync(userId, request);

        // Assert
        Assert.True(result.IsSuccess);
        await _userRepository.Received(1).GetActiveLegalRepresentativeIdAsync(companyId);
        await _userRepository.Received(1).InsertLegalRepresentativeAsync(Arg.Any<Guid>(), Arg.Any<string>(), Arg.Any<string>(), Arg.Any<string>(), Arg.Any<byte[]?>());
        await _userRepository.Received(1).GetActiveBankAccountIdAsync(companyId);
        await _userRepository.Received(1).InsertBankAccountAsync(Arg.Any<Guid>(), Arg.Any<string>(), Arg.Any<string>(), Arg.Any<string>());
    }

    #endregion

    #region Helper Methods

    private static Infrastructure.Core.Models.Company.Company CreateTestCompany(
        Guid? companyId = null,
        string ruc = "20123456789",
        string razonSocial = "Test Company S.A.C.",
        string domicilioLegal = "Av. Test 123, Lima",
        string? telefono = "987654321",
        string email = "test@company.com",
        bool isMype = false)
    {
        return new Infrastructure.Core.Models.Company.Company
        {
            CompanyId = companyId ?? Guid.NewGuid(),
            Ruc = ruc,
            RazonSocial = razonSocial,
            DomicilioLegal = domicilioLegal,
            Telefono = telefono,
            Email = email,
            IsMype = isMype,
            CreatedDate = DateTime.UtcNow,
            UpdatedDate = null
        };
    }

    private static CompanyDetails CreateTestCompanyDetails(
        Guid companyId,
        Guid? legalRepresentativeId = null,
        Guid? bankAccountId = null,
        byte[]? nationalIdImage = null)
    {
        var company = CreateTestCompany(companyId);

        LegalRepresentative? legalRep = null;
        if (legalRepresentativeId.HasValue)
        {
            legalRep = new LegalRepresentative
            {
                LegalRepresentativeId = legalRepresentativeId.Value,
                CompanyId = companyId,
                FullName = "John Doe",
                DocumentType = "DNI",
                DocumentNumber = "12345678",
                NationalIdImage = nationalIdImage,
                IsActive = true,
                CreatedDate = DateTime.UtcNow
            };
        }

        BankAccount? bankAccount = null;
        if (bankAccountId.HasValue)
        {
            bankAccount = new BankAccount
            {
                BankAccountId = bankAccountId.Value,
                CompanyId = companyId,
                BankName = "BCP",
                AccountNumber = "1234567890",
                CciCode = "12345678901234567890",
                IsActive = true,
                CreatedDate = DateTime.UtcNow
            };
        }

        return new CompanyDetails
        {
            Company = company,
            LegalRepresentative = legalRep,
            BankAccount = bankAccount
        };
    }

    private static UpdateCompanyDetailsRequest CreateUpdateRequest(
        Guid companyId,
        bool includeLegalRep = false,
        bool includeBankAccount = false,
        bool invalidBase64 = false)
    {
        UpdateLegalRepresentativeRequest? legalRep = null;
        if (includeLegalRep)
        {
            legalRep = new UpdateLegalRepresentativeRequest
            {
                FullName = "Jane Smith",
                DocumentType = "DNI",
                DocumentNumber = "87654321",
                NationalIdImage = invalidBase64 ? "invalid-base64!" : Convert.ToBase64String([1, 2, 3])
            };
        }

        UpdateBankAccountRequest? bankAccount = null;
        if (includeBankAccount)
        {
            bankAccount = new UpdateBankAccountRequest
            {
                BankName = "Interbank",
                AccountNumber = "9876543210",
                CciCode = "98765432109876543210"
            };
        }

        return new UpdateCompanyDetailsRequest
        {
            CompanyId = companyId,
            Ruc = "20123456789",
            RazonSocial = "Updated Company S.A.C.",
            DomicilioLegal = "Av. Updated 456, Lima",
            Telefono = "999888777",
            Email = "updated@company.com",
            FechaConstitucion = new DateTime(2020, 1, 1),
            IsMype = true,
            LegalRepresentative = legalRep,
            BankAccount = bankAccount
        };
    }

    #endregion
}