using FluentAssertions;
using Xunit;
using YachtSharing.MockGims.Services;

namespace YachtSharing.UnitTests;

public class GimsVerificationServiceTests
{
    private readonly GimsVerificationService _service;

    public GimsVerificationServiceTests()
    {
        _service = new GimsVerificationService();
    }

    [Theory]
    [InlineData("4512", "123456")]
    [InlineData("4512", "987654")]
    [InlineData("4500", "111122")]
    public void VerifyLicense_ReturnsTrue_ForPassportFromMockRegistry(string series, string number)
    {
        bool result = _service.VerifyLicense(series, number).IsValid;

        result.Should().BeTrue();
    }

    [Theory]
    [InlineData("4512", "000000")]
    [InlineData("0000", "111122")]
    [InlineData("1234", "567890")]
    public void VerifyLicense_ReturnsFalse_ForPassportOutsideMockRegistry(string series, string number)
    {
        bool result = _service.VerifyLicense(series, number).IsValid;

        result.Should().BeFalse();
    }
}
