using System;

namespace VoltElekto.Calendars
{
    /// <summary>
    /// Exceção ao tentar calcular no calendário além da maior ou menor data possível
    /// </summary>
    public class CalendarOutOfRangeException : ApplicationException
    {
        /// <summary>
        /// Construtor padrão da exceção
        /// </summary>
        /// <param name="calendar"></param>
        /// <param name="paramName"></param>
        /// <param name="outOfRangeDate"></param>
        public CalendarOutOfRangeException(ICalendar calendar, string paramName, DateTime outOfRangeDate) 
            :base($"Calendário '{calendar?.Name ?? string.Empty}' só pode calcular no período [{calendar.MinDate:yyyy-MM-dd}; {calendar.MaxDate:yyyy-MM-dd}], insuficiente para {outOfRangeDate:yyyy-MM-dd} informado em {paramName ?? string.Empty}.")
        {
            CalendarName = calendar.Name ?? string.Empty;
            OutOfRangeDate = outOfRangeDate;
            MinDate = calendar.MinDate;
            MaxDate = calendar.MaxDate;
        }

        /// <summary>
        /// Maior Data que o calendário suporta.
        /// </summary>
        public DateTime MaxDate { get; }

        /// <summary>
        /// Menor data que o sistema suporta
        /// </summary>
        public DateTime MinDate { get; }

        /// <summary>
        /// Data não suportada
        /// </summary>
        public DateTime OutOfRangeDate { get;  }

        /// <summary>
        /// Nome do Calendário
        /// </summary>
        public string CalendarName { get; }
    }
}