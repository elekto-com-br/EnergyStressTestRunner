using System;
using VoltElekto.Calendars;

namespace VoltElekto.Energy
{
    public class ForwardCurve : ICurve
    {
        /// <summary>
        /// Data de Referência
        /// </summary>
        public DateTime ReferenceDate { get; }

        /// <summary>
        /// A curva anterior
        /// </summary>
        private readonly ICurve _previousCurve;

        public ForwardCurve(DateTime referenceDate, ICurve previousCurve)
        {
            ReferenceDate = referenceDate;
            _previousCurve = previousCurve;
        }

        /// <summary>
        /// Obtém o valor numa data
        /// </summary>
        public double GetValue(DateTime date)
        {
            return _previousCurve.GetValue(date);
        }

        public ICalendar Calendar => _previousCurve.Calendar;

        /// <summary>
        /// Retorna o valor com o stress aplicado
        /// </summary>
        public (double zero, double parallelPlus, double parallelMinus, double shortPlus, double shortMinus, double ascendent, double descendent) GetValue(DateTime date, StressParameters stressParameters, PldLimits limits)
        {
            var price = GetValue(date);
            return stressParameters.GetValue(Calendar, ReferenceDate, date, price, limits);
        }

        /// <summary>
        /// Se é uma curva extrapolada
        /// </summary>
        public bool IsExtrapolated => true;
    }
}