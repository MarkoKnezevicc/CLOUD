using System;

namespace SmartMetering.AzureFunctions.Domain
{
    public enum TipTarife
    {
        VisaTarifa,  // VT: 07-23h
        NizaTarifa   // NT: 23-07h
    }

    public class Telemetrija
    {
        public Guid Id { get; set; }
        public int BrojiloId { get; set; }
        public DateTime VremeMerenja { get; set; }

        public decimal UkupnaPotrosnja { get; set; }     // kWh (stanje brojila)
        public decimal TrenutnoOpterecenje { get; set; } // kW
        public TipTarife Tarifa { get; set; }

        // Monofazni prikljucak
        public decimal? Napon { get; set; }
        public decimal? Struja { get; set; }
        public decimal? FaktorSnage { get; set; }

        // Trofazni prikljucak
        public decimal? NaponL1 { get; set; }
        public decimal? NaponL2 { get; set; }
        public decimal? NaponL3 { get; set; }
        public decimal? StrujaL1 { get; set; }
        public decimal? StrujaL2 { get; set; }
        public decimal? StrujaL3 { get; set; }
        public decimal? FaktorSnageL1 { get; set; }
        public decimal? FaktorSnageL2 { get; set; }
        public decimal? FaktorSnageL3 { get; set; }
    }
}