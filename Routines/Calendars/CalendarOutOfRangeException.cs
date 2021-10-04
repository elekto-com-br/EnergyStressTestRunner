using System;

namespace VoltElekto.Calendars
{
    /// <summary>
    /// Exce��o ao tentar calcular no calend�rio al�m da maior ou menor data poss�vel
    /// </summary>
    public class CalendarOutOfRangeException : ApplicationException
    {
        /// <summary>
        /// Construtor padr�o da exce��o
        /// </summary>
        /// <param name="calendar"></param>
        /// <param name="paramName"></param>
        /// <param name="outOfRangeDate"></param>
        public CalendarOutOfRangeException(ICalendar calendar, string paramName, DateTime outOfRangeDate) 
            :base($"Calend�rio '{calendar?.Name ?? string.Empty}' s� pode calcular no per�odo [{calendar.MinDate:yyyy-MM-dd}; {calendar.MaxDate:yyyy-MM-dd}], insuficiente para {outOfRangeDate:yyyy-MM-dd} informado em {paramName ?? string.Empty}.")
        {
            CalendarName = calendar.Name ?? string.Empty;
            OutOfRangeDate = outOfRangeDate;
            MinDate = calendar.MinDate;
            MaxDate = calendar.MaxDate;
        }

        /// <summary>
        /// Maior Data que o calend�rio suporta.
        /// </summary>
        public DateTime MaxDate { get; }

        /// <summary>
        /// Menor data que o sistema suporta
        /// </summary>
        public DateTime MinDate { get; }

        /// <summary>
        /// Data n�o suportada
        /// </summary>
        public DateTime OutOfRangeDate { get;  }

        /// <summary>
        /// Nome do Calend�rio
        /// </summary>
        public string CalendarName { get; }
    }
}