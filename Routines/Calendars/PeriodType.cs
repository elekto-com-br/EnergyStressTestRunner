using System.ComponentModel;

namespace VoltElekto.Calendars
{
    /// <summary>
    /// Tipos de per�odos
    /// </summary>
    /// <remarks>Valores sincronizados com os BDs. Cuidado ao alterar estes valores, ou os do BD (JP 2006-11-28)</remarks>
    public enum PeriodType
    {
        /// <summary>
        /// Dias �teis
        /// </summary>
        [Description("Dias �teis")]
        WorkDays = 0,

        /// <summary>
        /// Dias corridos
        /// </summary>
        [Description("Dias Corridos")]
        ActualDays = 1
    }
}