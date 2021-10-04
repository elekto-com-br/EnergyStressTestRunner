using System;
using VoltElekto.Calendars;

namespace VoltElekto.Energy
{
    /// <summary>
    /// Interface de Curvas
    /// </summary>
    public interface ICurve
    {
        /// <summary>
        /// Data de Referência
        /// </summary>
        DateTime ReferenceDate { get; }

        /// <summary>
        /// Obtém o valor numa data
        /// </summary>
        double GetValue(DateTime date);

        /// <summary>
        /// Se é uma curva extrapolada
        /// </summary>
        public bool IsExtrapolated { get; }

        /// <summary>
        /// O calendário usado
        /// </summary>
        public ICalendar Calendar { get; }

        /// <summary>
        /// Valores estressados na curva
        /// </summary>
        (double zero, double parallelPlus, double parallelMinus, double shortPlus, double shortMinus, double ascendent, double descendent) GetValue(DateTime date, StressParameters stressParameters, PldLimits limits);
    }
}