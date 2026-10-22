using FluentAssertions;
using Microsoft.AspNetCore.Mvc.Testing;
using System.Net;
using System.Net.Http;
using System.Net.Http.Json;
using System.Threading.Tasks;
using Xunit;
using YachtSharing.MockGims.Contracts;

namespace YachtSharing.IntegrationTests;

public class MockGimsIntegrationTests : IClassFixture<WebApplicationFactory<MockGimsApplicationMarker>>
{
    private readonly WebApplicationFactory<MockGimsApplicationMarker> _factory;

    public MockGimsIntegrationTests(WebApplicationFactory<MockGimsApplicationMarker> factory)
    {
        _factory = factory;
    }

    [Fact]
    public async Task Root_ReturnsServiceStatus()
    {
        HttpClient client = _factory.CreateClient();

        HttpResponseMessage response = await client.GetAsync("/");
        string content = await response.Content.ReadAsStringAsync();

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        content.Should().Contain("mock-gims");
        content.Should().Contain("ok");
    }

    [Fact]
    public async Task Verify_ReturnsTrue_ForPassportFromMockRegistry()
    {
        HttpClient client = _factory.CreateClient();

        VerifyLicenseResponse? response = await client.GetFromJsonAsync<VerifyLicenseResponse>(
            "/api/v1/gims/verify?series=4512&number=123456");

        response.Should().NotBeNull();
        bool? isValid = response?.IsValid;
        isValid.Should().BeTrue();
    }

    [Fact]
    public async Task Verify_ReturnsFalse_ForPassportOutsideMockRegistry()
    {
        HttpClient client = _factory.CreateClient();

        VerifyLicenseResponse? response = await client.GetFromJsonAsync<VerifyLicenseResponse>(
            "/api/v1/gims/verify?series=4512&number=000000");

        response.Should().NotBeNull();
        bool? isValid = response?.IsValid;
        isValid.Should().BeFalse();
    }

    [Fact]
    public async Task Verify_ReturnsBadRequest_WhenSeriesMissing()
    {
        HttpClient client = _factory.CreateClient();

        HttpResponseMessage response = await client.GetAsync("/api/v1/gims/verify?number=123456");
        string content = await response.Content.ReadAsStringAsync();

        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
        content.Should().Contain("series");
    }

    [Fact]
    public async Task Verify_ReturnsBadRequest_WhenNumberMissing()
    {
        HttpClient client = _factory.CreateClient();

        HttpResponseMessage response = await client.GetAsync("/api/v1/gims/verify?series=4512");
        string content = await response.Content.ReadAsStringAsync();

        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
        content.Should().Contain("number");
    }

    [Fact]
    public async Task Verify_ReturnsBadRequest_WhenPassportLengthIsInvalid()
    {
        HttpClient client = _factory.CreateClient();

        HttpResponseMessage response = await client.GetAsync("/api/v1/gims/verify?series=4512&number=12345");
        string content = await response.Content.ReadAsStringAsync();

        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
        content.Should().Contain("exactly 10 digits");
    }

    [Fact]
    public async Task Verify_ReturnsBadRequest_WhenPassportContainsNonDigits()
    {
        HttpClient client = _factory.CreateClient();

        HttpResponseMessage response = await client.GetAsync("/api/v1/gims/verify?series=45AB&number=123456");
        string content = await response.Content.ReadAsStringAsync();

        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
        content.Should().Contain("exactly 10 digits");
    }
}
