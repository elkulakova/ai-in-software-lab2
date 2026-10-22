using System;
using System.Net.Http;
using System.Net.Http.Json;
using System.Threading;
using System.Threading.Tasks;
using YachtSharing.BookingService.Application.Abstractions.Services;
using YachtSharing.BookingService.Infrastructure.Exceptions;

namespace YachtSharing.BookingService.Infrastructure.Services;

public class GimsIntegrationService : IGimsIntegrationService
{
    private readonly HttpClient _httpClient;

    public GimsIntegrationService(HttpClient httpClient)
    {
        _httpClient = httpClient;
    }

    public async Task<bool> VerifySkipperLicenseAsync(
        string series,
        string number,
        CancellationToken cancellationToken = default)
    {
        try
        {
            string url = $"api/v1/gims/verify?series={series}&number={number}";

            HttpResponseMessage response = await _httpClient.GetAsync(url, cancellationToken);

            if (!response.IsSuccessStatusCode)
            {
                return false;
            }

            GimsVerificationResult? result = await response.Content.ReadFromJsonAsync<GimsVerificationResult>(cancellationToken: cancellationToken);
            return result?.IsValid ?? false;
        }
        catch (Exception ex)
        {
            throw new GimsIntegrationException($"GIMS registry communication failed: {ex.Message}", ex);
        }
    }

    private sealed record GimsVerificationResult(bool IsValid);
}