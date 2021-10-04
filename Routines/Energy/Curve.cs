using System;
using System.Collections.Generic;
using System.Linq;
using VoltElekto.Calendars;

namespace VoltElekto.Energy
{
    /// <summary>
    /// Curva de Preço de Energia
    /// </summary>
    /// <remarks>
    /// Interpolação Linear por dias úteis
    /// Extrapolação flat
    /// </remarks>
    public class Curve : ICurve
    {
        /// <summary>
        /// Para Interpolação
        /// </summary>
        private readonly int[] _maturities;

        /// <summary>
        /// Para Interpolação
        /// </summary>
        private readonly (DateTime date, int maturity, double price)[] _vertex;

        /// <summary>
        /// Cache por data
        /// </summary>
        private readonly Dictionary<DateTime, (DateTime date, int maturity, double price)> _vertexByDate;

        /// <summary>
        /// Cache por prazo
        /// </summary>
        private readonly Dictionary<int, (DateTime date, int maturity, double price)> _vertexByMaturity;

        /// <summary>
        /// Calendário
        /// </summary>
        private readonly ICalendar _calendar;

        /// <summary>
        /// Data de Referência
        /// </summary>
        public DateTime ReferenceDate { get; }

        
        /// <summary>
        /// Constrói a curva
        /// </summary>
        public Curve(DateTime referenceDate, ICalendar calendar, IEnumerable<(DateTime date, double price)> prices)
        {
            ReferenceDate = referenceDate;
            _calendar = calendar;

            Dictionary<DateTime, (DateTime date, int maturity, double price)> values = new();

            foreach (var (date, price) in prices.Where(dp => dp.date >= referenceDate))
            {
                var maturity = calendar.GetDeltaWorkDays(referenceDate, date);
                values[date] = (date, maturity, price);
            }

            if (values.Count == 0)
            {
                throw new ArgumentException($"A curva de energia em {referenceDate:yyyy-MM-dd} deve ter pelo menos 1 ponto válido.");
            }

            // Cache rápido e exato por data
            _vertexByDate = values;
            _vertexByMaturity = values.Values.ToDictionary(p => p.maturity);

            // Para Interpolações
            _maturities = values.Values.OrderBy(p => p.maturity).Select(p => p.maturity).ToArray();
            _vertex = values.Values.OrderBy(p => p.maturity).ToArray();

        }

        /// <summary>
        /// Obtém o valor numa data
        /// </summary>
        public double GetValue(DateTime date)
        {
            if (_vertexByDate.TryGetValue(date, out var value))
            {
                return value.price;
            }

            var maturity = _calendar.GetDeltaWorkDays(ReferenceDate, date);
            return GetValue(maturity);
        }

        /// <summary>
        /// O calendário
        /// </summary>
        public ICalendar Calendar => _calendar;

        /// <summary>
        /// Retorna o valor com o stress aplicado
        /// </summary>
        public (double zero, double parallelPlus, double parallelMinus, double shortPlus, double shortMinus, double ascendent, double descendent) GetValue(DateTime date, StressParameters stressParameters, PldLimits limits)
        {
            var price = GetValue(date);
            return stressParameters.GetValue(_calendar, ReferenceDate, date, price, limits);
        }

        /// <summary>
        /// Se é uma curva extrapolada
        /// </summary>
        public bool IsExtrapolated => false;

        /// <summary>
        /// Obtém o valor num prazo
        /// </summary>
        private double GetValue(int maturity)
        {

            if (_vertexByMaturity.TryGetValue(maturity, out var value))
            {
                return value.price;
            }

            // Antes do primeiro
            if (maturity <= _vertex[0].maturity)
            {
                return _vertex[0].price;
            }

            // Depois do último
            if (maturity >= _vertex[_vertex.Length - 1].maturity)
            {
                return _vertex[_vertex.Length - 1].price;
            }

            // Índice necessário
            var i = Array.BinarySearch(_maturities, maturity);

            if (i >= 0)
            {
                // Achado exatamente... não deveria acontecer
                return _vertex[i].price;
            }

            var n = ~i;
            if (n >= _maturities.Length)
            {
                n = _maturities.Length - 1;
            }

            var p = n - 1;
            if (p < 0)
            {
                throw new InvalidOperationException($"Erro na interpolação da curva em {ReferenceDate:yyyy-MM-dd} para o prazo {maturity}!");
            }

            // Interpolação Linear
            double pm = _vertex[p].maturity;
            var pp = _vertex[p].price;

            double nm = _vertex[n].maturity;
            var np = _vertex[n].price;

            var dm = nm - pm;
            var dp = np - pp;
            var slope = dp / dm;
            var d = slope * (maturity - pm);
            var price = pp + d;

            return price;
        }

    }
}