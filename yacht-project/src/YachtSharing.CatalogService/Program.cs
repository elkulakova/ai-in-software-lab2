using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using System;
using System.Linq;
using YachtSharing.CatalogService.DataTable;
using YachtSharing.CatalogService.Entities;
using YachtSharing.CatalogService.Grpc;
using YachtSharing.CatalogService.Repositories;

namespace YachtSharing.CatalogService;

public class Program
{
    public static void Main(string[] args)
    {
        WebApplicationBuilder builder = WebApplication.CreateBuilder(args);

        builder.WebHost.ConfigureKestrel(options =>
        {
            options.ListenAnyIP(8080, listenOptions =>
            {
                listenOptions.Protocols = Microsoft.AspNetCore.Server.Kestrel.Core.HttpProtocols.Http1;
            });

            options.ListenAnyIP(8082, listenOptions =>
            {
                listenOptions.Protocols = Microsoft.AspNetCore.Server.Kestrel.Core.HttpProtocols.Http2;
            });
        });

        builder.Services.AddControllers();

        builder.Services.AddDbContext<CatalogDbContext>(options =>
            options.UseNpgsql(builder.Configuration.GetConnectionString("CatalogDb")));

        builder.Services.AddScoped<IYachtRepository, YachtRepository>();

        builder.Services.AddGrpc();
        builder.Services.AddEndpointsApiExplorer();
        builder.Services.AddSwaggerGen();

        WebApplication app = builder.Build();

        app.UseSwagger();
        app.UseSwaggerUI();

        app.MapGrpcService<CatalogGrpcService>();
        app.MapControllers();
        InitializeDatabase(app);

        app.MapGet("/health", CheckHealthStatus);

        app.Run();
    }

    public static IResult CheckHealthStatus(CatalogDbContext db)
    {
        bool isDbWorking = db.Database.CanConnect();

        string dbStatus = "not connected";
        string commonStatus = "error";

        if (isDbWorking)
        {
            dbStatus = "connected";
            commonStatus = "ok";
        }

        return Results.Ok(new
        {
            service = "catalog",
            status = commonStatus,
            timestamp = DateTimeOffset.UtcNow,
            checks = new { database = dbStatus },
        });
    }

    public static void InitializeDatabase(WebApplication app)
    {
        try
        {
            using IServiceScope scope = app.Services.CreateScope();
            CatalogDbContext db = scope.ServiceProvider.GetRequiredService<CatalogDbContext>();

            db.Database.EnsureCreated();

            bool hasYachts = db.Yachts.Any();
            if (hasYachts == false)
            {
                var yacht1 = new Yacht { Model = "MJ360", PricePerHour = 5000 };
                var yacht2 = new Yacht { Model = "Anastasia", PricePerHour = 7500 };
                var yacht3 = new Yacht { Model = "R5678", PricePerHour = 6000 };
                var yacht4 = new Yacht { Model = "McQueen", PricePerHour = 6500 };
                var yacht5 = new Yacht { Model = "UltraSound", PricePerHour = 7000 };

                db.Yachts.Add(yacht1);
                db.Yachts.Add(yacht2);
                db.Yachts.Add(yacht3);
                db.Yachts.Add(yacht4);
                db.Yachts.Add(yacht5);
                db.SaveChanges();

                var booking1 = new BookingProjection
                {
                    YachtId = yacht1.Id,
                    StartTime = new DateTimeOffset(2026, 6, 20, 10, 0, 0, TimeSpan.Zero),
                    EndTime = new DateTimeOffset(2026, 6, 20, 12, 0, 0, TimeSpan.Zero),
                };

                var booking2 = new BookingProjection
                {
                    YachtId = yacht2.Id,
                    StartTime = new DateTimeOffset(2026, 6, 20, 14, 0, 0, TimeSpan.Zero),
                    EndTime = new DateTimeOffset(2026, 6, 20, 16, 0, 0, TimeSpan.Zero),
                };

                db.Bookings.AddRange(booking1, booking2);
                db.SaveChanges();

                Console.WriteLine("Тестовые данные записаны в БД");
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine("Ошибка инициализации БД: " + ex.Message);
        }
    }
}
