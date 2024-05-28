using Microsoft.AspNetCore.Http.HttpResults;
using Newtonsoft.Json;
using PoFN.models;
using System.Net.Http.Headers;

namespace PoFN
{
    public class Program
    {
        private static bool useApi = true;
        private static FuelApiData fuelApiData = new();
        private static DateTime fuelDataLastUpdate = DateTime.UtcNow;
        private static TimeSpan updateInterval = TimeSpan.FromMinutes(30);

        private const int AuthRetries = 1;

        private static HttpClient httpClient = new();

        private static ApiKeys apiKeys = new();
        private static string OAuthToken = string.Empty;

        //Only call at app startup
        public static async Task<string> GetAllPricesJson(int iteration = 0)
        {
            using HttpRequestMessage request = new(HttpMethod.Get, "http://api.onegov.nsw.gov.au/FuelPriceCheck/v1/fuel/prices");
            request.Headers.Add("Authorization", "Bearer " + OAuthToken);
            request.Headers.Add("Content-Type", "application/json; charset=utf-8");

            var response = await httpClient.SendAsync(request);

            if(response.IsSuccessStatusCode)
            {
                return await response.Content.ReadAsStringAsync();
            }
            else if (response.StatusCode == System.Net.HttpStatusCode.Unauthorized && iteration <= AuthRetries)
            {
                OAuthToken = GenerateOAuthToken(apiKeys.AuthHeader).Result.AccessToken;
                return await GetAllPricesJson(iteration++);
            }
            else
            {
                Console.WriteLine($"GetAllPrices failed. HTTP status code was {{{response.StatusCode}}}");
                return string.Empty;
            }
        }

        public static async Task<string> GetUpdatedPricesJson(int iteration = 0)
        {
            using HttpRequestMessage request = new(HttpMethod.Get, "http://api.onegov.nsw.gov.au/oauth/client_credential/accesstoken?grant_type=client_credentials");
            request.Headers.Add("Authorization", "Bearer " + OAuthToken);
            request.Headers.Add("Content-Type", "application/json; charset=utf-8");

            var response = await httpClient.SendAsync(request);

            if (response.IsSuccessStatusCode)
            {
                return await response.Content.ReadAsStringAsync();
            }
            else if (response.StatusCode == System.Net.HttpStatusCode.Unauthorized && iteration <= AuthRetries)
            {
                OAuthToken = GenerateOAuthToken(apiKeys.AuthHeader).Result.AccessToken;
                return await GetUpdatedPricesJson(iteration++);
            }
            else
            {
                Console.WriteLine($"GetAllPrices failed. HTTP status code was {{{response.StatusCode}}}");
                return string.Empty;
            }
        }

        public static async void CheckAndUpdateFuelData()
        {
            if(useApi && DateTime.UtcNow - fuelDataLastUpdate >= updateInterval)
            {
                Console.WriteLine("Updating fuel price data...");
                //Update api data
            }
        }

        public static async Task<ApiAccessToken> GenerateOAuthToken(string authHeader)
        {
            using HttpRequestMessage request = new(HttpMethod.Get, "http://api.onegov.nsw.gov.au/oauth/client_credential/accesstoken?grant_type=client_credentials");
            request.Headers.Add("grant_type", "client_credentials");
            request.Headers.Add("Authorization", "Basic" + authHeader);

            var response = await httpClient.SendAsync(request);
            if(response.IsSuccessStatusCode)
            {
                string json = response.Content.ReadAsStringAsync().Result;
                return JsonConvert.DeserializeObject<ApiAccessToken>(json) ?? new();
            }
            Console.WriteLine("Could not generate access token");
            return new();
        }

        public static void Main(string[] args)
        {
            if(args.Length > 0 && args[0] == "loadFromFile")
            {
                useApi = false;
            }

            var builder = WebApplication.CreateBuilder(args);

            // Add services to the container.
            builder.Services.AddAuthorization();

            // Learn more about configuring Swagger/OpenAPI at https://aka.ms/aspnetcore/swashbuckle
            builder.Services.AddEndpointsApiExplorer();
            builder.Services.AddSwaggerGen();
            builder.Services.AddHttpClient();

            var app = builder.Build();

            // Configure the HTTP request pipeline.
            if (app.Environment.IsDevelopment())
            {
                app.UseSwagger();
                app.UseSwaggerUI();
            }

            app.UseHttpsRedirection();

            app.UseAuthorization();

            #region GetFuelData
            if (useApi)
            {
                //Load API keys
                using (StreamReader r = new("keys.json"))
                {
                    string json = r.ReadToEnd();
                    apiKeys = JsonConvert.DeserializeObject<ApiKeys>(json) ?? new();
                }
                OAuthToken = GenerateOAuthToken(apiKeys.AuthHeader).Result.AccessToken;

                //Save fuel price data to file for debugging idk
                using (StreamWriter r = new("prices.json"))
                {
                    string jsonApiData = GetAllPricesJson().Result;
                    fuelApiData = JsonConvert.DeserializeObject<FuelApiData>(jsonApiData) ?? new();
                    r.Write(jsonApiData);
                }
            }
            else
            {
                //Read fuel price data from file
                using StreamReader r = new("prices.json");
                fuelApiData = JsonConvert.DeserializeObject<FuelApiData>(r.ReadToEnd()) ?? new();
            }

            #endregion GetFuelData

            app.MapGet("/ok", (HttpContext httpContext) =>
            {
                return Results.Ok();
            })
            .WithName("Ok")
            .WithOpenApi();

            app.MapGet("/stationPricesRadius", (HttpContext httpContext, double latitude, double longitude, double radius, string fuelType = "Any") =>
            {
                lock (fuelApiData)
                {
                    CheckAndUpdateFuelData();
                    return Results.Ok(fuelApiData.GetStationPricesWithinRadius(new(latitude, longitude), 10000, fuelType));
                }
            })
            .WithName("StationPricesInRadius")
            .WithOpenApi();

            if (app.Environment.IsDevelopment())
            {
                app.MapGet("/stationPricesRadiusDev", (HttpContext httpContext, string fuelType = "Any") =>
                {
                    Location location = new(-33.4970376, 151.3159292);
                    lock (fuelApiData)
                    {
                        CheckAndUpdateFuelData();
                        return fuelApiData.GetStationPricesWithinRadius(location, 10000, fuelType);
                    }
                })
                .WithName("GetStationsInRangeDev")
                .WithOpenApi();
            }

            app.Run();
        }
    }
}
