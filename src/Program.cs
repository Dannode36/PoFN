using Microsoft.AspNetCore.Mvc;
using PoFN.models;
using PoFN.services;

namespace PoFN
{
    public class Program
    {
        public static void Main(string[] args)
        {
            var builder = WebApplication.CreateBuilder(args);

            // Add services to the container.
            builder.Services.AddAuthorization();

            // Learn more about configuring Swagger/OpenAPI at https://aka.ms/aspnetcore/swashbuckle
            builder.Services.AddEndpointsApiExplorer();
            builder.Services.AddSwaggerGen();
            builder.Services.AddHttpClient();
            builder.Services.AddSingleton<IFuelPriceService, FuelPriceService>();

            var app = builder.Build();

            // Configure the HTTP request pipeline.
            if (app.Environment.IsDevelopment())
            {
                app.UseSwagger();
                app.UseSwaggerUI();
            }

            //app.UseHttpsRedirection();

            app.UseAuthorization();

            app.MapGet("/stations/{code}", ([FromServices] IFuelPriceService fpService, string code) =>
            {
                return fpService.GetStationPrices(code);
            })
            .WithName("StationPrices")
            .WithOpenApi();

            app.MapGet("/stations/radius", ([FromServices] IFuelPriceService fpService, double latitude, double longitude, double radius, string fuelType = "Any") =>
            {
                return fpService.GetStationPricesWithinRadius(new(latitude, longitude), radius, fuelType);
            })
            .WithName("StationPricesInRadius")
            .WithOpenApi();

            if (app.Environment.IsDevelopment())
            {
                app.MapGet("/stationPricesRadiusDev", ([FromServices] IFuelPriceService fpService, string fuelType = "Any") =>
                {
                    Location location = new(-33.4970376, 151.3159292);
                    return fpService.GetStationPricesWithinRadius(location, 10000, fuelType);
                })
                .WithName("GetStationsInRangeDev")
                .WithOpenApi();

                app.MapGet("/data", ([FromServices] IFuelPriceService fpService) =>
                {
                    return fpService.GetAllData();
                })
                .WithName("Data")
                .WithOpenApi();
            }

            app.Run();
        }
    }
}
