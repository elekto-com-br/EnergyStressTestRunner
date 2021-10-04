using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using VoltElekto.Calendars;

namespace VoltElekto.Energy
{
    /// <summary>
    /// Um servidor de curvas que lê de um arquivo texto
    /// </summary>
    public class CurveServerFromTextFile : ICurveServer
    {
        /// <summary>
        /// Calendário
        /// </summary>
        private readonly ICalendar _calendar;

        /// <summary>
        /// Cache das curvas construídas
        /// </summary>
        private readonly Dictionary<DateTime, ICurve> _curves = new();

        /// <summary>
        /// Datas explicitas das curvas (não extrapoladas)
        /// </summary>
        private readonly DateTime[] _dates;

        public CurveServerFromTextFile(string fileName, ICalendar calendar)
        {
            _calendar = calendar;
            if (!File.Exists(fileName))
            {
                throw new FileNotFoundException("O arquivo de curvas não foi encontrado", fileName);
            }

            var prices = File.ReadAllLines(fileName)
                .Skip(1).Where(l=> !string.IsNullOrWhiteSpace(l))
                .Select(l => l.Split('\t')).Where(a => a.Length == 3)
                .Select(a => (date: DateTime.ParseExact(a[0], "yyyy-MM-dd", CultureInfo.InvariantCulture), endDate: DateTime.ParseExact(a[1], "yyyy-MM-dd", CultureInfo.InvariantCulture), value: double.Parse(a[2], NumberStyles.Any, CultureInfo.InvariantCulture)))
                .Where(ddv => calendar.IsWorkday(ddv.date))
                .GroupBy(ddv => ddv.date).ToDictionary(g => g.Key, g => g.Select(ddv => (ddv.endDate, ddv.value)).ToList());

            foreach (var kv in prices)
            {
                var curve = new Curve(kv.Key, _calendar, kv.Value);
                _curves[kv.Key] = curve;
            }

            _dates = _curves.Keys.OrderBy(d => d).ToArray();

        }

        /// <summary>
        /// Maior Data Explícita
        /// </summary>
        public DateTime MaxDate => _dates[_dates.Length - 1];

        /// <summary>
        /// Menor Data Explícita
        /// </summary>
        public DateTime MinDate => _dates[0];

        /// <summary>
        /// Obtém a curva numa data
        /// </summary>
        public ICurve GetCurve(DateTime referenceDate)
        {
            referenceDate = _calendar.GetPrevOrSameWorkday(referenceDate);

            if (_curves.TryGetValue(referenceDate, out var curve))
            {
                return curve;
            }

            // Uma curva forward será usada...

            if (referenceDate < _dates[0])
            {
                throw new ArgumentOutOfRangeException(nameof(referenceDate), referenceDate, "Não existem dados anteriores ou iguais a para construir a curva de energia.");
            }

            // Índice necessário
            var i = Array.BinarySearch(_dates, referenceDate);

            if (i >= 0)
            {
                throw new InvalidOperationException($"Uma curva em {referenceDate:yyyy-MM-dd} deveria existir explicitamente.");
            }

            // Índice do anterior
            i = ~i;
            i -= 1;

            var previousDate = _dates[i];
            if (!_curves.TryGetValue(previousDate, out curve))
            {
                throw new InvalidOperationException($"Uma curva em {referenceDate:yyyy-MM-dd} deveria existir explicitamente em cache.");
            }

            var fwdCurve = new ForwardCurve(referenceDate, curve);
            _curves[referenceDate] = fwdCurve;

            return fwdCurve;
        }

    }
}