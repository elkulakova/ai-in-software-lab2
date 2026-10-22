using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using YachtSharing.MockGims.Contracts;
using YachtSharing.MockGims.Services;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddSingleton<GimsVerificationService>();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

var app = builder.Build();

app.UseSwagger();
app.UseSwaggerUI();

app.MapGet("/", () => Results.Ok(new
{
    service = "mock-gims",
    status = "ok",
}));

app.MapGet("/api/v1/gims/verify", IResult (string? series, string? number, GimsVerificationService verificationService) =>
{
    if (string.IsNullOrWhiteSpace(series))
    {
        return Results.BadRequest("Query parameter 'series' is required.");
    }

    if (string.IsNullOrWhiteSpace(number))
    {
        return Results.BadRequest("Query parameter 'number' is required.");
    }

    if (HasValidPassportFormat(series, number) == false)
    {
        return Results.BadRequest("Passport series and number must contain exactly 10 digits together.");
    }

    VerifyLicenseResponse response = verificationService.VerifyLicense(series, number);
    return Results.Ok(response);
});

app.Run();

static bool HasValidPassportFormat(string series, string number)
{
    string passport = series + number;

    if (passport.Length != 10)
    {
        return false;
    }

    foreach (char symbol in passport)
    {
        if (char.IsDigit(symbol) == false)
        {
            return false;
        }
    }

    return true;
}

public sealed class MockGimsApplicationMarker
{
}

public partial class Program
{
}