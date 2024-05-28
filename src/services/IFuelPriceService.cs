using PoFN.models;

namespace PoFN.services
{
    public interface IFuelPriceService
    {
        FuelApiData GetAllData();
        List<FuelPrice> GetStationPrices(string stationcode);
        List<StationPrices> GetStationPricesWithinRadius(Location location, double radius, string fuelType = "Any");
        List<Station> GetStationsWithinRadius(Location location, double radius);
    }
}
