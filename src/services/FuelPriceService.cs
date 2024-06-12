using Microsoft.Extensions.Logging;
using Newtonsoft.Json;
using PoFN.models;

namespace PoFN.services
{
    public class FuelPriceService : IFuelPriceService
    {
        public int FuelApiCallCount { get; private set; } = 0;
        public int ThisApiCallCount { get; private set; } = 0;

        private readonly FuelApiData fuelApiData;
        private DateTime fuelDataLastUpdate;
        private readonly TimeSpan updateInterval = TimeSpan.FromMinutes(30);
        public const string anyFuelType = "Any";

        private readonly HttpClient httpClient;
        private const int AuthRetries = 1;
        private readonly ApiKeys apiKeys;
        private string AccessToken = string.Empty;

        private readonly ILogger _logger;

        public FuelPriceService(ILogger<FuelPriceService> logger)
        {
            _logger = logger;
            httpClient = new();

            //Load API keys
            using (StreamReader r = new("keys.json"))
            {
                string json = r.ReadToEnd();
                apiKeys = JsonConvert.DeserializeObject<ApiKeys>(json) ?? new();
            }

            //Generate access token for the next function
            AccessToken = GenerateOAuthToken(apiKeys.AuthHeader).Result.AccessToken;

            //Get current fuel prices from NSW API
            string jsonApiData = GetAllPricesJson().Result;
            if (jsonApiData != string.Empty)
            {
                fuelApiData = JsonConvert.DeserializeObject<FuelApiData>(jsonApiData) ?? new();
                fuelDataLastUpdate = DateTime.UtcNow;
            }
            else
            {
                fuelApiData = new();
            }
        }

        private async Task<ApiAccessToken> GenerateOAuthToken(string authHeader)
        {
            using HttpRequestMessage request = new(HttpMethod.Get, "https://api.onegov.nsw.gov.au/oauth/client_credential/accesstoken?grant_type=client_credentials");
            request.Headers.Add("Authorization", authHeader);

            var response = await httpClient.SendAsync(request);
            FuelApiCallCount++;

            if (response.IsSuccessStatusCode)
            {
                var accessToken = JsonConvert.DeserializeObject<ApiAccessToken>(response.Content.ReadAsStringAsync().Result);
                if(accessToken != null)
                {
                    return accessToken;
                }
            }
            _logger.LogError(
                "Could not generate access token\n" +
                response.Content.ReadAsStringAsync().Result
                );
            return new();
        }
        private async Task<string> GetAllPricesJson(int iteration = 0)
        {
            using HttpRequestMessage request = new(HttpMethod.Get, "https://api.onegov.nsw.gov.au/FuelPriceCheck/v1/fuel/prices");
            request.Headers.Add("Authorization", "Bearer " + AccessToken);
            request.Headers.Add("apikey", apiKeys.ApiKey);
            request.Headers.Add("transactionid", Guid.NewGuid().ToString());
            request.Headers.Add("requesttimestamp", DateTime.UtcNow.ToString("dd/MM/yyyy HH:mm:ss tt"));
            if (!request.Headers.TryAddWithoutValidation("Content-Type", "application/json; charset=utf-8"))
            {
                _logger.LogWarning("Could not add \"Content-Type\" header");
            }

            var response = await httpClient.SendAsync(request);
            FuelApiCallCount++;

            if (response.IsSuccessStatusCode)
            {
                return await response.Content.ReadAsStringAsync();
            }
            else if (response.StatusCode == System.Net.HttpStatusCode.Unauthorized && iteration < AuthRetries)
            {
                _logger.LogWarning(
                    $"GetAllPricesJson failed. HTTP status code was {response.StatusCode}. Retrying...\n" +
                    response.Content.ReadAsStringAsync().Result);

                await Task.Delay(500);

                AccessToken = GenerateOAuthToken(apiKeys.AuthHeader).Result.AccessToken;
                return await GetAllPricesJson(++iteration);
            }
            else
            {
                _logger.LogError(
                    $"GetAllPricesJson failed. HTTP status code was {{{response.StatusCode}}}\n" +
                    response.Content.ReadAsStringAsync().Result);
                return string.Empty;
            }
        }
        private async Task<string> GetUpdatedPricesJson(int iteration = 0)
        {
            using HttpRequestMessage request = new(HttpMethod.Get, "https://api.onegov.nsw.gov.au/FuelPriceCheck/v1/fuel/prices/new");
            request.Headers.Add("Authorization", "Bearer " + AccessToken);
            request.Headers.Add("apikey", apiKeys.ApiKey);
            request.Headers.Add("transactionid", Guid.NewGuid().ToString());
            request.Headers.Add("requesttimestamp", DateTime.UtcNow.ToString("dd/MM/yyyy HH:mm:ss tt"));
            if (!request.Headers.TryAddWithoutValidation("Content-Type", "application/json; charset=utf-8"))
            {
                _logger.LogWarning("Could not add \"Content-Type\" header");
            }

            var response = await httpClient.SendAsync(request);
            FuelApiCallCount++;

            if (response.IsSuccessStatusCode)
            {
                return await response.Content.ReadAsStringAsync();
            }
            else if (response.StatusCode == System.Net.HttpStatusCode.Unauthorized && iteration < AuthRetries)
            {
                _logger.LogWarning(
                    $"GetUpdatedPricesJson failed. HTTP status code was {response.StatusCode}. Retrying...\n" +
                    response.Content.ReadAsStringAsync().Result);

                await Task.Delay(500);

                AccessToken = GenerateOAuthToken(apiKeys.AuthHeader).Result.AccessToken;
                return await GetUpdatedPricesJson(++iteration);
            }
            else
            {
                _logger.LogError(
                    $"GetUpdatedPricesJson failed. HTTP status code was {{{response.StatusCode}}}\n" +
                    response.Content.ReadAsStringAsync().Result);
                return string.Empty;
            }
        }
        private async void CheckAndUpdateFuelData()
        {
            if (DateTime.UtcNow - fuelDataLastUpdate >= updateInterval)
            {
                Console.WriteLine("Updating fuel price data...");
                FuelApiData updatedFuelData = JsonConvert.DeserializeObject<FuelApiData>(await GetUpdatedPricesJson()) ?? new();
                fuelDataLastUpdate = DateTime.UtcNow;

                int stationsUpdated = 0;
                int stationsAdded = 0;
                int pricesUpdated = 0;
                int pricesAdded = 0;

                //Update stations
                foreach (var station in updatedFuelData.Stations)
                {
                    int updateIndex = fuelApiData.Stations.FindIndex(x => x.Code == station.Code);
                    if (updateIndex >= 0)
                    {
                        fuelApiData.Stations[updateIndex] = station;
                        stationsUpdated++;
                    }
                    else
                    {
                        fuelApiData.Stations.Add(station);
                        stationsAdded++;
                    }
                }

                //Update prices
                foreach (var price in updatedFuelData.Prices)
                {
                    int updateIndex = fuelApiData.Prices.FindIndex(x => x.Stationcode == price.Stationcode);
                    if (updateIndex >= 0)
                    {
                        fuelApiData.Prices[updateIndex] = price;
                        pricesUpdated++;
                    }
                    else
                    {
                        fuelApiData.Prices.Add(price);
                        pricesAdded++;
                    }
                }

                _logger.LogInformation($"Fuel data updated successfully:\n" +
                    $"\tStations Updated: {stationsUpdated}" +
                    $"\tStations Added: {stationsAdded}" +
                    $"\tPrices Updated: {pricesUpdated}" +
                    $"\tPrices Added: {pricesAdded}");
                _logger.LogInformation($"Fuel API call count: {FuelApiCallCount}");

            }
        }

        public FuelApiData GetAllData()
        {
            ThisApiCallCount++;
            _logger.LogInformation($"This API call count: {ThisApiCallCount}");
            return fuelApiData;
        }
        public List<Station> GetStationsWithinRadius(Location location, double radius)
        {
            lock (fuelApiData)
            {
                CheckAndUpdateFuelData();
                ThisApiCallCount++;
                _logger.LogInformation($"This API call count: {ThisApiCallCount}");
                return fuelApiData.Stations.Where(x => Geolocation.CalculateDistance(location, x.Location) <= radius).ToList();
            }
        }
        public StationPrices GetStationPrices(string stationcode)
        {
            lock (fuelApiData)
            {
                CheckAndUpdateFuelData();
                ThisApiCallCount++;
                _logger.LogInformation($"This API call count: {ThisApiCallCount}");
                return new()
                {
                    Station = fuelApiData.Stations.Where(x => x.Code == stationcode).FirstOrDefault(),
                    Prices = fuelApiData.Prices.Where(x => x.Stationcode == stationcode).ToList()
                };
            }
        }
        public List<StationPrices> GetStationPricesWithinRadius(Location location, double radius, string fuelType = anyFuelType)
        {
            lock (fuelApiData)
            {
                CheckAndUpdateFuelData();
                ThisApiCallCount++;
                _logger.LogInformation($"This API call count: {ThisApiCallCount}");

                List<StationPrices> stationPrices = [];

                foreach (var station in fuelApiData.GetStationsWithinRadius(location, radius))
                {
                    List<FuelPrice> prices = fuelApiData.GetStationPrices(station.Code).Where(x => fuelType == anyFuelType || x.Fueltype == fuelType).ToList();

                    if (prices.Count > 0)
                    {
                        StationPrices stationPrice = new()
                        {
                            Station = station,
                            Prices = prices
                        };
                        stationPrices.Add(stationPrice);
                    }
                }
                return stationPrices;
            }
        }
    }
}
