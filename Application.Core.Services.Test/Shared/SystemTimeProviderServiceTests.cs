using Application.Core.Interfaces.Shared;
using Application.Core.Services.Shared;

namespace Application.Core.Services.Test.Shared;

public sealed class SystemTimeProviderServiceTests
{
    private readonly ITimeProvider _sut;

    public SystemTimeProviderServiceTests()
    {
        _sut = new SystemTimeProviderService();
    }

    [Fact]
    public void UtcNow_ReturnsCurrentUtcTime()
    {
        // Arrange
        var beforeCall = DateTime.UtcNow;

        // Act
        var result = _sut.UtcNow;

        // Assert
        var afterCall = DateTime.UtcNow;

        Assert.Equal(DateTimeKind.Utc, result.Kind);
        Assert.InRange(result, beforeCall.AddSeconds(-1), afterCall.AddSeconds(1));
    }

    [Fact]
    public void Now_ReturnsCurrentLocalTime()
    {
        // Arrange
        var beforeCall = DateTime.Now;

        // Act
        var result = _sut.Now;

        // Assert
        var afterCall = DateTime.Now;

        Assert.NotEqual(DateTimeKind.Utc, result.Kind);
        Assert.InRange(result, beforeCall.AddSeconds(-1), afterCall.AddSeconds(1));
    }

    [Fact]
    public void UtcNow_CalledMultipleTimes_ReturnsProgressingTime()
    {
        // Act
        var first = _sut.UtcNow;
        Thread.Sleep(10); // Small delay
        var second = _sut.UtcNow;

        // Assert
        Assert.True(second >= first, "Second call should return time equal to or after the first call");
    }

    [Fact]
    public void Now_CalledMultipleTimes_ReturnsProgressingTime()
    {
        // Act
        var first = _sut.Now;
        Thread.Sleep(10); // Small delay
        var second = _sut.Now;

        // Assert
        Assert.True(second >= first, "Second call should return time equal to or after the first call");
    }

    [Fact]
    public void UtcNow_And_Now_AreDifferentByTimezoneOffset()
    {
        // Act
        var utcTime = _sut.UtcNow;
        var localTime = _sut.Now;

        // Assert
        // The difference should be approximately equal to the timezone offset
        var timeDifference = localTime - utcTime;
        var expectedOffset = TimeZoneInfo.Local.GetUtcOffset(DateTime.Now);

        // Allow 2 seconds tolerance for execution time
        Assert.InRange(timeDifference.TotalSeconds,
            expectedOffset.TotalSeconds - 2,
            expectedOffset.TotalSeconds + 2);
    }
}