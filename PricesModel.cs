using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace PoFN
{
    public class StationPriceData
    {
        public List<Station> Stations { get; set; } = [];
        public List<FuelPrice> Prices { get; set; } = [];

        public List<Station> GetStationsWithinRadius(Location location, double radius)
        {
            return Stations.Where(x => Geolocation.CalculateDistance(location, x.Location) <= radius).ToList();
        }

        public List<FuelPrice> GetStationPrices(string stationcode)
        {
            return Prices.Where(x => x.Stationcode == stationcode).ToList();
        }

        //x.Fueltype == (fuelType == "Any" ? x.Fueltype : fuelType) has to be the smartest dumb shit I've ever written
        //fuelType == "Any" ? true : x.Fueltype == fuelType only benefits from being slightly shorter :')
        //(fuelType == "Any" || x.Fueltype == fuelType) is so much better: wtf me... thanks intellisense
        public List<StationPrices> GetStationPricesWithinRadius(Location location, double radius, string fuelType = "Any")
        {
            List<StationPrices> stationPrices = [];

            foreach (var station in GetStationsWithinRadius(location, radius))
            {
                List<FuelPrice> prices = Prices.Where(x => x.Stationcode == station.Code && (fuelType == "Any" || x.Fueltype == fuelType)).ToList();

                if(prices.Count > 0)
                {
                    StationPrices stationPrice = new()
                    {
                        Station = station,
                        Prices = GetStationPrices(station.Code).Where(x => fuelType == "Any" || x.Fueltype == fuelType).ToList()
                    };
                    stationPrices.Add(stationPrice);
                }
            }
            return stationPrices;
        }
    }

    public struct StationPrices
    {
        public Station Station { get; set; }
        public List<FuelPrice> Prices { get; set; }
    }

    //FOLLOWING USED FOR THE NSW FUEL API. DO NOT CHANGE
    public struct Station
    {
        public string Brandid { get; set; }
        public string Stationid { get; set; }
        public string Brand { get; set; }
        public string Code { get; set; }
        public string Name { get; set; }
        public string Address { get; set; }
        public Location Location { get; set; }
        public bool IsAdBlueAvailable { get; set; }
    }

    public struct Location(double latitude, double longitude)
    {
        public double Latitude { get; set; } = latitude;
        public double Longitude { get; set; } = longitude;
    }

    public struct FuelPrice
    {
        public string Stationcode { get; set; }
        public string Fueltype { get; set; }
        public double Price { get; set; }
        public string Lastupdated { get; set; }
    }
}
