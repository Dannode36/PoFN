using PoFN.models;

namespace PoFN.services
{
    public interface IFuelPriceService
    {
        FuelApiData GetAllData();
        StationPrices? GetStationPrices(int stationcode);
        List<Station> GetStationsWithinRadius(Location location, double radius);
        List<StationPrices> GetStationPricesWithinRadius(Location location, double radius, string fuelTypes = "Any");
    }
}
