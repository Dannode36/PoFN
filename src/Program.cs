using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.AspNetCore.Mvc;
using Newtonsoft.Json;
using PoFN.models;
using PoFN.services;
using System.Net.Http.Headers;

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

            app.MapGet("/ok", (HttpContext httpContext) =>
            {
                return Results.Ok();
            })
            .WithName("Ok")
            .WithOpenApi();

            app.MapGet("/stationPricesRadius", (HttpContext httpContext, [FromServices] FuelPriceService fpService, double latitude, double longitude, double radius, string fuelType = "Any") =>
            {
                return fpService.GetStationPricesWithinRadius(new(latitude, longitude), 10000, fuelType);
            })
            .WithName("StationPricesInRadius")
            .WithOpenApi();

            if (app.Environment.IsDevelopment())
            {
                app.MapGet("/stationPricesRadiusDev", (HttpContext httpContext, [FromServices] IFuelPriceService fpService, string fuelType = "Any") =>
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
