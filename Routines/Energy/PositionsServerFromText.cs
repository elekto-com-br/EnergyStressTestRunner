using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text;
using VoltElekto.Calendars;
using VoltElekto.Market;

namespace VoltElekto.Energy
{
    public class PositionsServerFromText : IPositionsServer
    {
        private readonly ICalendar _calendar;
        private readonly string[] _lines;

        public PositionsServerFromText(string fileName, ICalendar calendar)
        {
            _calendar = calendar;
            _lines = File.ReadAllLines(fileName, Encoding.UTF8);
        }

        public IEnumerable<EnergyPosition> GetPositions()
        {
            // Pulando o header
            var rowNumber = 1;
            foreach (var line in _lines.Skip(1))
            {
                ++rowNumber;

                if (string.IsNullOrWhiteSpace(line))
                {
                    // Pula em branco
                    continue;
                }

                if (line.StartsWith("#") || line.StartsWith("//") || line.StartsWith("--"))
                {
                    // Pula comentários
                    continue;
                }

                var fields = line.Split('\t');
                if (fields.Length < 3)
                {
                    throw new ArgumentOutOfRangeException(nameof(fields.Length), fields.Length,
                        $"Linha {rowNumber}: Uma linha de dados deve ter ao menos 3 campos separados por tabulações.");
                }

                if (!DateTime.TryParseExact(fields[0], "yyyy-MM-dd", CultureInfo.InvariantCulture, DateTimeStyles.None, out var referenceDate))
                {
                    throw new ArgumentOutOfRangeException(nameof(fields.Length), fields.Length,
                        $"Linha {rowNumber}, Campo 1: A data de referência deve estar no formato yyyy-MM-dd.");
                }
                referenceDate = _calendar.GetPrevOrSameWorkday(referenceDate);

                if (!DateTime.TryParseExact(fields[1], "yyyy-MM-dd", CultureInfo.InvariantCulture, DateTimeStyles.None, out var deliveryDate))
                {
                    throw new ArgumentOutOfRangeException(nameof(fields.Length), fields.Length,
                        $"Linha {rowNumber}, Campo 2: A data de entrega deve estar no formato yyyy-MM-dd.");
                }

                if (!double.TryParse(fields[2], NumberStyles.Any, CultureInfo.InvariantCulture, out var volume))
                {
                    throw new ArgumentOutOfRangeException(nameof(fields.Length), fields.Length,
                        $"Linha {rowNumber}, Campo 3: O volume deve estar no formato inglês, ponto como separador de milhar.");
                }

                if (volume == 0)
                {
                    // Posição irrelevante
                    continue;
                }

                var buySell = BuySell.Buy;
                if (volume < 0)
                {
                    buySell = BuySell.Sell;
                    volume *= -1;
                }

                var position = new EnergyPosition
                {
                    ReferenceDate = referenceDate,
                    StartMonth = deliveryDate.Date.StartOfMonth(),
                    BuySell = buySell,
                    Amount = volume,
                    Tag = $"r{rowNumber:00000}"
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