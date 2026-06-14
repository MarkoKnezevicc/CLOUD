using System;

namespace SmartMetering.AzureFunctions.Infrastructure.Persistence
{
    public class TelemetrijaEntity : BaseTableEntity
    {
        public DateTime VremeMerenja { get; set; }

        public double UkupnaPotrosnja { get; set; }     // kWh
        public double TrenutnoOpterecenje { get; set; } // kW
        public string Tarifa { get; set; } = string.Empty;

        // Monofazni prikljucak
        public double? Napon { get; set; }
        public double? Struja { get; set; }
        public double? FaktorSnage { get; set; }

        // Trofazni prikljucak
        public double? NaponL1 { get; set; }
        public double? NaponL2 { get; set; }
        public double? NaponL3 { get; set; }
        public double? StrujaL1 { get; set; }
        public double? StrujaL2 { get; set; }
        public double? StrujaL3 { get; set; }
        public double? FaktorSnageL1 { get; set; }
        public double? FaktorSnageL2 { get; set; }
        public double? FaktorSnageL3 { get; set; }
    }
}