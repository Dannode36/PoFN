namespace PoFN.models
{
    public struct Station
    {
        public Station() { }

        public string Brandid { get; set; } = string.Empty;
        public string Stationid { get; set; } = string.Empty;
        public string Brand { get; set; } = string.Empty;
        public string Code { get; set; } = string.Empty;
        public string Name { get; set; } = string.Empty;
        public string Address { get; set; } = string.Empty;
        public Location Location { get; set; } = default;
        public bool IsAdBlueAvailable { get; set; } = default;
    }
}
