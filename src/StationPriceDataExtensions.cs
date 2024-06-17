using System.Diagnostics;
using PoFN.models;

namespace PoFN
{
    public static class StationPriceDataExtensions
    {
        public static List<Station> GetStationsWithinRadius(this FuelApiData spData, Location location, double radius)
        {
            return spData.Stations.Where(x => Geolocation.CalculateDistance(location, x.Location) <= radius).ToList();
        }

        public static List<FuelTypePrice> GetStationPrices(this FuelApiData spData, string stationcode)
        {
            return spData.Prices.Where(x => x.Stationcode == stationcode).ToList();
        }

        //x.Fueltype == (fuelType == "Any" ? x.Fueltype : fuelType) has to be the smartest dumb shit I've ever written
        //fuelType == "Any" ? true : x.Fueltype == fuelType only benefits from being slightly shorter :')
        //(fuelType == "Any" || x.Fueltype == fuelType) is so much better: wtf me... thanks intellisense
        public static List<StationPrices> GetStationPricesWithinRadius(this FuelApiData spData, Location location, double radius, string fuelType = "Any")
        {
            List<StationPrices> stationPrices = [];

            foreach (var station in spData.GetStationsWithinRadius(location, radius))
            {
                List<FuelTypePrice> prices = spData.GetStationPrices(station.Code).Where(x => fuelType == "Any" || x.Fueltype == fuelType).ToList();

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
