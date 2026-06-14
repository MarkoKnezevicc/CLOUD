using System;
using SmartMetering.AzureFunctions.Domain;

namespace SmartMetering.AzureFunctions.Infrastructure.Persistence
{
    public static class TelemetrijaMapper
    {
        public static TelemetrijaEntity ToEntity(Telemetrija model)
        {
            string reverseTicks = (DateTime.MaxValue.Ticks - model.VremeMerenja.Ticks).ToString("d19");
            string rowKey = $"{reverseTicks}_{model.Id}";

            return new TelemetrijaEntity
            {
                PartitionKey = model.BrojiloId.ToString(),
                RowKey = rowKey,
                VremeMerenja = DateTime.SpecifyKind(model.VremeMerenja, DateTimeKind.Utc),
                UkupnaPotrosnja = (double)model.UkupnaPotrosnja,
                TrenutnoOpterecenje = (double)model.TrenutnoOpterecenje,
                Tarifa = model.Tarifa.ToString(),

                Napon = (double?)model.Napon,
                Struja = (double?)model.Struja,
                FaktorSnage = (double?)model.FaktorSnage,

                NaponL1 = (double?)model.NaponL1,
                NaponL2 = (double?)model.NaponL2,
                NaponL3 = (double?)model.NaponL3,
                StrujaL1 = (double?)model.StrujaL1,
                StrujaL2 = (double?)model.StrujaL2,
                StrujaL3 = (double?)model.StrujaL3,
                FaktorSnageL1 = (double?)model.FaktorSnageL1,
                FaktorSnageL2 = (double?)model.FaktorSnageL2,
                FaktorSnageL3 = (double?)model.FaktorSnageL3
            };
        }

        public static Telemetrija ToDomain(TelemetrijaEntity entity)
        {
            // RowKey format: "{reverseTicks(19 cifara)}_{Guid}"
            string[] delovi = entity.RowKey.Split('_', 2);
            Guid id = Guid.Parse(delovi[1]);

            return new Telemetrija
            {
                Id = id,
                BrojiloId = int.Parse(entity.PartitionKey),
                VremeMerenja = entity.VremeMerenja,
                UkupnaPotrosnja = (decimal)entity.UkupnaPotrosnja,
                TrenutnoOpterecenje = (decimal)entity.TrenutnoOpterecenje,
                Tarifa = Enum.Parse<TipTarife>(entity.Tarifa),

                Napon = (decimal?)entity.Napon,
                Struja = (decimal?)entity.Struja,
                FaktorSnage = (decimal?)entity.FaktorSnage,

                NaponL1 = (decimal?)entity.NaponL1,
                NaponL2 = (decimal?)entity.NaponL2,
                NaponL3 = (decimal?)entity.NaponL3,
                StrujaL1 = (decimal?)entity.StrujaL1,
                StrujaL2 = (decimal?)entity.StrujaL2,
                StrujaL3 = (decimal?)entity.StrujaL3,
                FaktorSnageL1 = (decimal?)entity.FaktorSnageL1,
                FaktorSnageL2 = (decimal?)entity.FaktorSnageL2,
                FaktorSnageL3 = (decimal?)entity.FaktorSnageL3
            };
        }
    }
}