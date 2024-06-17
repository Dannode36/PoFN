using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace PoFN.models
{
    public struct FuelTypePrice
    {
        public string Stationcode { get; set; }
        public string Fueltype { get; set; }
        public double Price { get; set; }
        public string Lastupdated { get; set; }
    }
}
