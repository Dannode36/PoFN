namespace PoFN.models
{
    public struct StationPrices
    {
        public StationPrices() { }

        public Station Station { get; set; } = new();
        public List<FuelPrice> Prices { get; set; } = [];
    }
}
