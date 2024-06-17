namespace PoFN.models
{
    public struct StationPrices
    {
        public StationPrices() { }

        public Station Station { get; set; } = new();
        public List<FuelTypePrice> Prices { get; set; } = [];
    }
}
