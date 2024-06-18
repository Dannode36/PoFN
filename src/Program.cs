using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.AspNetCore.Mvc;
using PoFN.models;
using PoFN.services;

using LoggingField = Microsoft.AspNetCore.HttpLogging.HttpLoggingFields;

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
            builder.Services.AddHttpLogging(logging =>
            {
                logging.LoggingFields = 
                LoggingField.RequestQuery | LoggingField.RequestMethod | LoggingField.RequestPath
                | LoggingField.ResponsePropertiesAndHeaders | LoggingField.ResponseStatusCode;
            });

            var app = builder.Build();

            // Configure the HTTP request pipeline.
            if (app.Environment.IsDevelopment())
            {
                app.UseSwagger();
                app.UseSwaggerUI();
            }

            //app.UseHttpsRedirection();
            app.UseHttpLogging();
            app.UseAuthorization();

            app.MapGet("/stations/{code}", ([FromServices] IFuelPriceService fpService, [FromRoute] int code) =>
            {
                return Results.Ok(fpService.GetStationPrices(code));
            })
            .WithName("StationPrices")
            .WithOpenApi();

            app.MapGet("/stations/radius", ([FromServices] IFuelPriceService fpService, [FromQuery] double latitude, [FromQuery] double longitude, [FromQuery] double radius, [FromQuery] string fuelTypes = "Any") =>
            {
                List<string> fuelTypeList = [.. fuelTypes.Split('+', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)];
                if (fuelTypeList.Count == 0) 
                { 
                    return Results.BadRequest("fuelTypes parameter was invalid"); 
                }

                return Results.Ok(fpService.GetStationPricesWithinRadius(new(latitude, longitude), radius, fuelTypeList));
            })
            .WithName("StationPricesInRadius")
            .WithOpenApi();

            if (app.Environment.IsDevelopment())
            {
                app.MapGet("/stationPricesRadiusDev", ([FromServices] IFuelPriceService fpService, string fuelTypes = "Any") =>
                {
                    List<string> fuelTypeList = [.. fuelTypes.Split('+', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)];
                    if (fuelTypeList.Count == 0) 
                    { 
                        return Results.BadRequest("fuelTypes parameter was invalid"); 
                    }

                    Location location = new(-33.4970376, 151.3159292);
                    return Results.Ok(fpService.GetStationPricesWithinRadius(location, 10000, fuelTypeList));
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
