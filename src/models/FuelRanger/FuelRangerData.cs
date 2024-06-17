namespace PoFN.models.FuelRanger
{
    public struct FuelRangerData
    {
        public Dictionary<string, List<FuelPrice>> PriceMap { get; set; } //Fuel type -> List of prices
        public List<Station> Stations { get; set; }
    }
}
