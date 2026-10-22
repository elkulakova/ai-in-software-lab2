using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using System;
using System.Collections.Generic;
using YachtSharing.BookingService;
using YachtSharing.BookingService.Application.Abstractions.UseCases;

namespace YachtSharing.IntegrationTests;

public class BookingServiceWebApplicationFactory : WebApplicationFactory<BookingApplicationMarker>
{
    private readonly IBookingService _bookingService;

    public BookingServiceWebApplicationFactory(IBookingService bookingService)
    {
        _bookingService = bookingService;
    }

    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        builder.ConfigureAppConfiguration((_, configurationBuilder) =>
        {
            var values = new Dictionary<string, string?>
            {
                ["ConnectionStrings:DefaultConnection"] = "Host=localhost;Port=5432;Database=yacht_booking;Username=postgres;Password=postgres",
                ["IntegrationServices:CatalogGrpcUrl"] = "http://localhost:5002",
                ["IntegrationServices:GimsBaseUrl"] = "http://localhost:7001/",
            };

            configurationBuilder.AddInMemoryCollection(values);
        });

        builder.ConfigureServices(services =>
        {
            services.RemoveAll<IBookingService>();
            services.AddSingleton(_bookingService);
        });
    }
}
