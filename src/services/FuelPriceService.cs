using Microsoft.Extensions.Logging;
using Newtonsoft.Json;
using PoFN.models;
using PoFN.models.FuelRanger;

namespace PoFN.services
{
    public class FuelPriceService : IFuelPriceService
    {
        public int FuelApiCallCount { get; private set; } = 0;

        private readonly FuelApiData fuelApiData;
        private DateTime fuelDataLastUpdate;
        private readonly TimeSpan updateInterval = TimeSpan.FromMinutes(30);
        public const string anyFuelType = "Any";
        public const string E10 = "E10";

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
            return fuelApiData;
        }
        public List<Station> GetStationsWithinRadius(Location location, double radius)
        {
            lock (fuelApiData)
            {
                CheckAndUpdateFuelData();
                return fuelApiData.Stations.Where(x => Geolocation.CalculateDistance(location, x.Location) <= radius).ToList();
            }
        }
        public StationPrices? GetStationPrices(int stationcode)
        {
            lock (fuelApiData)
            {
                if(fuelApiData.Stations.Any(x => x.Code == stationcode.ToString()))
                {
                    CheckAndUpdateFuelData();
                    return new()
                    {
                        Station = fuelApiData.Stations.Where(x => x.Code == stationcode.ToString()).FirstOrDefault(),
                        Prices = fuelApiData.Prices.Where(x => x.Stationcode == stationcode.ToString()).ToList()
                    };
                }
                return null;
            }
        }
        public List<StationPrices> GetStationPricesWithinRadius(Location location, double radius, string fuelTypes = anyFuelType)
        {
            lock (fuelApiData)
            {
                CheckAndUpdateFuelData();

                List<StationPrices> stationPrices = [];

                foreach (var station in fuelApiData.GetStationsWithinRadius(location, radius))
                {
                    List<FuelTypePrice> prices = fuelApiData.GetStationPrices(station.Code).Where(x => fuelTypes == anyFuelType || x.Fueltype == fuelTypes).ToList();

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

                if (fuelTypes == anyFuelType)
                {
                    var hasE10 = stationPrices.Where(x => x.Prices.Any(x => x.Fueltype == E10));;
                    var sortedHasE10O = hasE10.OrderBy(x => x.Prices.FirstOrDefault(x => x.Fueltype == E10).Price);
                    var noE10 = stationPrices.Where(x => !x.Prices.Any(x => x.Fueltype == E10));;
                    return [.. sortedHasE10O, .. noE10];
                }
                else
                {
                    return stationPrices.OrderBy(x => x.Prices.FirstOrDefault(x => x.Fueltype == fuelTypes).Price).ToList();
                }
            }
        }

        //More convenient api for FuelRanger (weird transform bullshit happening beware)
        public FuelRangerData GetFuelPricesWithinRadius(Location location, double radius, string fuelTypes = anyFuelType)
        {
            List<string> fuelTypeList = [.. fuelTypes.Split('+', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)];
            if(fuelTypeList.Count == 0) { return new(); }

            FuelRangerData fuelData = new();

            lock (fuelApiData)
            {
                CheckAndUpdateFuelData();

                List<StationPrices> stationPrices = [];

                foreach (var station in fuelApiData.GetStationsWithinRadius(location, radius))
                {
                    List<FuelTypePrice> prices = fuelApiData.GetStationPrices(station.Code).Where(x => fuelTypes == anyFuelType || x.Fueltype == fuelTypes).ToList();

                    if (fuelTypeList.Contains("Any"))
                    {

                    }
                    else
                    {
                        foreach (var fuelType in fuelTypeList)
                        {
                            if (fuelData.PriceMap.TryGetValue(fuelType, out var priceMap))
                            {
                                FuelTypePrice fuelTypePrice = fuelApiData.GetStationPrices(station.Code).Where(x => x.Fueltype == fuelType).First();
                                FuelPrice fuelPrice = new() //FuelRanger object
                                {
                                    Stationcode = fuelTypePrice.Stationcode,
                                    Price = fuelTypePrice.Price,
                                    Lastupdated = fuelTypePrice.Lastupdated
                                };
                                priceMap.Add(fuelPrice);
                            }
                            else
                            {

                            }
                        }
                    }
                }

                //return stationPrices.OrderBy(x => x.Prices.FirstOrDefault(x => x.Fueltype == fuelTypes).Price).ToList();
            }
        }
    }
}
