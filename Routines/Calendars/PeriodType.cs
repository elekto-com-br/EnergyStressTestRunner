using System.ComponentModel;

namespace VoltElekto.Calendars
{
    /// <summary>
    /// Tipos de períodos
    /// </summary>
    /// <remarks>Valores sincronizados com os BDs. Cuidado ao alterar estes valores, ou os do BD (JP 2006-11-28)</remarks>
    public enum PeriodType
    {
        /// <summary>
        /// Dias úteis
        /// </summary>
        [Description("Dias Úteis")]
        WorkDays = 0,

        /// <summary>
        /// Dias corridos
        /// </summary>
        [Description("Dias Corridos")]
        ActualDays = 1
    }
}