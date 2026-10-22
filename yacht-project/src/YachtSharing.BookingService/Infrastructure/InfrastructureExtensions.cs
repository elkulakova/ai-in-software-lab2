using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using System;
using YachtSharing.BookingService.Application.Abstractions.Repositories;
using YachtSharing.BookingService.Application.Abstractions.Services;
using YachtSharing.BookingService.Infrastructure.BackgroundServices;
using YachtSharing.BookingService.Infrastructure.Persistence;
using YachtSharing.BookingService.Infrastructure.Persistence.Repositories;
using YachtSharing.BookingService.Infrastructure.Services;

namespace YachtSharing.BookingService.Infrastructure;

public static class InfrastructureExtensions
{
    public static IServiceCollection AddInfrastructure(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        string? connectionString = configuration.GetConnectionString("DefaultConnection");

        services.AddDbContext<BookingDbContext>(options =>
            options.UseNpgsql(connectionString));

        services.AddScoped<IUnitOfWork, UnitOfWork>();

        services.AddScoped<IBookingRepository, BookingRepository>();
        services.AddScoped<IClientRepository, ClientRepository>();
        services.AddScoped<ICrewRepository, CrewRepository>();
        services.AddScoped<IPaymentRepository, PaymentRepository>();

        services.AddScoped<IDateTimeProvider, DateTimeProvider>();

        AppContext.SetSwitch("System.Net.Http.SocketsHttpHandler.Http2UnencryptedSupport", true);

        services.AddGrpcClient<Catalog.Catalog.CatalogClient>(options =>
        {
            string catalogUrl = configuration["IntegrationServices:CatalogGrpcUrl"] ?? "http://localhost:5002";
            options.Address = new Uri(catalogUrl);
        }).ConfigurePrimaryHttpMessageHandler(() =>
        {
            var handler = new System.Net.Http.SocketsHttpHandler
            {
                EnableMultipleHttp2Connections = true,
            };
            return handler;
        });

        services.AddScoped<ICatalogIntegrationService, CatalogIntegrationService>();

        services.AddHttpClient<IGimsIntegrationService, GimsIntegrationService>(client =>
        {
            string gimsUrl = configuration["IntegrationServices:GimsBaseUrl"] ?? "https://localhost:7001/";
            client.BaseAddress = new Uri(gimsUrl);
        });

        services.AddScoped<Application.Abstractions.UseCases.IBookingService, Application.Services.BookingManagementService>();

        services.AddHostedService<BookingExpirationWorker>();

        return services;
    }
}
