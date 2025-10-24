using Application.Core.Interfaces.Company;
using Application.Core.Services.Company;
using Global.Objects.Company;
using Global.Objects.Errors;
using Global.Objects.Results;
using Infrastructure.Core.Interfaces.Account;
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

    #endregion
}