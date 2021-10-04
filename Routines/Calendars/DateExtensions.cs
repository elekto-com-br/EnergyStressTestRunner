using System;
using System.Runtime.CompilerServices;

namespace VoltElekto.Calendars
{
    /// <summary>
    ///     Utilitários para lidar com Datas, em especial removendo a informação de localidade usualmente presente.
    /// </summary>
    public static class DateExtensions
    {
        /// <summary>
        ///     Hoje, mas sem informação de localidade (DateTimeKind.Unspecified)
        /// </summary>
        /// <value>The today.</value>
        public static DateTime Today => DateTime.SpecifyKind(DateTime.Today, DateTimeKind.Unspecified);

        /// <summary>
        ///     Remove a informação de tipo da data. Passo importante caso as datas tenham de trafegar via SOAP entre máquinas em
        ///     diferentes TimeZones.
        /// </summary>
        public static DateTime RemoveKind(this DateTime date)
        {
            return DateTime.SpecifyKind(date, DateTimeKind.Unspecified);
        }

        /// <summary>
        ///     Retorna somente a parte de data e sem especificação de tipo
        /// </summary>
        public static DateTime NormalizeDate(this DateTime date)
        {
            return DateTime.SpecifyKind(date.Date, DateTimeKind.Unspecified);
        }

        /// <summary>
        ///     Cria uma data a partir de ser componentes, mas sem informação de localidade (DateTimeKind.Unspecified)
        /// </summary>
        /// <param name="year">The year.</param>
        /// <param name="month">The month.</param>
        /// <param name="day">The day.</param>
        /// <param name="toPastDirection">Se true o dia existente anterior (no caso de dadas impossíveis) é retornado</param>
        /// <returns>O dia</returns>
        [MethodImpl(MethodImplOptions.NoInlining)]
        private static DateTime CreateDate(int year, int month, int day, bool toPastDirection = false)
        {
            if (day < 1)
            {
                day = 1;
            }

            NormalizeYearAndMonth(ref year, ref month);

            if (day > DateTime.DaysInMonth(year, month))
            {
                // Dia impossível
                var adjustedDate = CreateDate(year, month + 1, 1);
                if (toPastDirection)
                {
                    adjustedDate = adjustedDate.AddDays(-1);
                }

                return adjustedDate;
            }

            // Dia possível
            return new DateTime(year, month, day, 0, 0, 0, DateTimeKind.Unspecified);
        }

        private static void NormalizeYearAndMonth(ref int year, ref int month)
        {
            var serialMonth = GetSerialMonth(year, month);
            GetYearAndMonthFromSerialMonth(serialMonth, out year, out month);
        }

        private static int GetSerialMonth(int year, int month)
        {
            return year * 12 + (month - 1);
        }

        private static void GetYearAndMonthFromSerialMonth(int serialMonth, out int year, out int month)
        {
            year = serialMonth / 12;
            month = serialMonth - year * 12 + 1;
        }

        /// <summary>
        ///     Primeiro dia do mês.
        /// </summary>
        /// <param name="date">Data inicial</param>
        /// <param name="months">Número de meses para frente ou para trás. Use 0 para mês corrente.</param>
        /// <returns></returns>
        public static DateTime StartOfMonth(this DateTime date, int months = 0)
        {
            var d = CreateDate(date.Year, date.Month, 1);
            return d.AddMonths(months);
        }

        /// <summary>
        /// Número de Horas num mês
        /// </summary>
        /// <remarks>
        /// Nenhuma consideração quanto a horário de verão no Brasil
        /// </remarks>
        public static int HoursInMonth(this DateTime date)
        {
            return DateTime.DaysInMonth(date.Year, date.Month) * 24;
        }
    }
}