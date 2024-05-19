
using Newtonsoft.Json;

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

            var app = builder.Build();

            // Configure the HTTP request pipeline.
            if (app.Environment.IsDevelopment())
            {
                app.UseSwagger();
                app.UseSwaggerUI();
            }

            app.UseHttpsRedirection();

            app.UseAuthorization();

            //Load fuel price data
            StationPriceData stationPriceData;
            using (StreamReader r = new("prices.json"))
            {
                string json = r.ReadToEnd();
                stationPriceData = JsonConvert.DeserializeObject<StationPriceData>(json) ?? new();
            }

            app.MapGet("/stations", (HttpContext httpContext, string fuelType = "All") =>
            {
                Location location = new(-33.4970376, 151.3159292);
                return stationPriceData.GetStationPricesWithinRadius(location, 10000, fuelType);
            })
            .WithName("GetStationsInRange")
            .WithOpenApi();

            app.Run();
        }
    }
}
