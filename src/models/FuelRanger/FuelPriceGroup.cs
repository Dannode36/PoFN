namespace PoFN.models.FuelRanger
{
    public struct FuelPriceGroup
    {
        public string FuelType {  get; set; }
        public List<FuelPrice> Prices { get; set; }
    }
}
