namespace PoFN.models
{
    public class FuelApiData
    {
        public List<Station> Stations { get; set; } = [];
        public List<FuelTypePrice> Prices { get; set; } = [];
    }
}
