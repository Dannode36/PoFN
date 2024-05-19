namespace PoFN.models
{
    public struct Location(double latitude, double longitude)
    {
        public double Latitude { get; set; } = latitude;
        public double Longitude { get; set; } = longitude;
    }
}
