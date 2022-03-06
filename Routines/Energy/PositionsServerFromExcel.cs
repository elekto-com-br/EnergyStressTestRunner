using System;
using System.Collections.Generic;
using System.Linq;
using VoltElekto.Calendars;
using VoltElekto.Excel;
using VoltElekto.Market;

namespace VoltElekto.Energy
{
    /// <summary>
    /// Devolve posições (ou trades) a partir de um Excel
    /// </summary>
    public class PositionsServerFromExcel : ExcelReaderBase, IPositionsServer
    {
        private readonly ICalendar _calendar;

        public PositionsServerFromExcel(string fileName, ICalendar calendar)
        : base(false, fileName)
        {
            _calendar = calendar;
            UseVbaCompatibleReferences = true;
        }

        public IEnumerable<EnergyPosition> GetPositions()
        {
            const string sheetName = "Posições";
            if (!WorksheetExists(sheetName))
            {
                throw new ApplicationException($"O excel '{ExcelFile}' deve conter uma planilha chamada exatamente '{sheetName}'. Veja o arquivo exemplo.");
            }

            const int startRow = 10;

            var referenceDates = ReadDateTimeColumn(sheetName, startRow, AutomaticEnd, 1);

            if (referenceDates.Length == 0)
            {
                yield break;
            }
            
            var endRow = startRow + referenceDates.Length - 1;

            var deliveryDates = ReadDateTimeColumn(sheetName, startRow, endRow, 2);
            var buySells = ReadStringColumn(sheetName, startRow, endRow, 3);
            var volumes = ReadDoubleColumn(sheetName, startRow, endRow, 4);

            for (var i = 0; i < referenceDates.Length; i++)
            {
                var referenceDate = _calendar.GetPrevOrSameWorkday(referenceDates[i]);
                var deliveryDate = deliveryDates[i];
                var buySellText = buySells[i];
                var volume = volumes[i];

                // Normaliza Compra e Venda

                if (volume == 0)
                {
                    // Volume zero é perda de tempo, nem lê
                    continue;
                }

                if (string.IsNullOrWhiteSpace(buySellText))
                {
                    if (volume > 0)
                    {
                        buySellText = "c";
                    }
                    else if (volume < 0)
                    {
                        buySellText = "v";
                        volume *= -1;
                    }
                }

                var buySell = BuySellExtensions.Parse(buySellText);
                switch (buySell)
                {
                    case BuySell.Buy when volume < 0:
                        volume *= -1;
                        buySell = BuySell.Sell;
                        break;
                    case BuySell.Sell when volume < 0:
                        volume *= -1;
                        buySell = BuySell.Buy;
                        break;
                }

                var position = new EnergyPosition
                {
                    ReferenceDate = referenceDate,
                    StartMonth = deliveryDate.Date.StartOfMonth(),
                    BuySell = buySell,
                    Amount = volume,
                    Tag = $"r{startRow + i:00000}"
                };

                yield return position;
            }

        }

        public IEnumerable<EnergyPosition> GetTrades()
        {
            throw new NotImplementedException();
        }
    }
}