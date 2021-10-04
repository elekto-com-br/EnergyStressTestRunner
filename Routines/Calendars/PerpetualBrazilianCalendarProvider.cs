using System;
using System.Collections.Generic;

namespace VoltElekto.Calendars
{
    public static class PerpetualBrazilianCalendarProvider 
    {
        
        public static ICalendar GetCalendar()
        {
            return new Calendar(GetHolidays(DateTime.Today.AddYears(-75), DateTime.Today.AddYears(+75)), "br", "br");
        }

        /// <summary>
        ///     Retorna feriados contidos nos anos delimitados pelos parâmetros
        /// </summary>
        /// <param name="initialDate">data minima</param>
        /// <param name="finalDate">data maxima</param>
        /// <returns></returns>
        private static IEnumerable<DateTime> GetHolidays(DateTime initialDate, DateTime finalDate)
        {
            var holidays = new List<DateTime>(100);

            // Faz de ano em ano
            var yearInitial = initialDate.Year;
            var yearFinal = finalDate.Year;

            for (var year = yearInitial; year <= yearFinal; ++year)
            {
                // Calcula o dia da Pascoa
                GetXyForYear(year, out var x, out var y);

                var a = year%19;
                var b = year%4;
                var c = year%7;
                var d = (19*a + x)%30;
                var e = (2*b + 4*c + 6*d + y)%7;

                int day, month;
                if ((d + e) > 9)
                {
                    day = (d + e - 9);
                    month = 4;
                }
                else
                {
                    day = (d + e + 22);
                    month = 3;
                }

                if ((day == 26) && (month == 4))
                {
                    day = 19;
                }
                if ((day == 25) && (month == 4) && (d == 28) && (a > 10))
                {
                    day = 18;
                }

                var easterDay = new DateTime(year, month, day, 0, 0, 0, DateTimeKind.Unspecified);

                // 2a feira de carnaval
                var holiday = easterDay.AddDays(-48);
                holidays.Add(holiday);

                // 3a feira de carnaval
                holiday = easterDay.AddDays(-47);
                holidays.Add(holiday);

                // 6a feira da paixão
                holiday = easterDay.AddDays(-2);
                holidays.Add(holiday);

                // Corpus Christi
                holiday = easterDay.AddDays(60);
                holidays.Add(holiday);

                // Confraternização
                holiday = new DateTime(year, 1, 1, 0, 0, 0, DateTimeKind.Unspecified);
                holidays.Add(holiday);

                // Tiradentes
                holiday = new DateTime(year, 4, 21, 0, 0, 0, DateTimeKind.Unspecified);
                holidays.Add(holiday);

                // Trabalho
                holiday = new DateTime(year, 5, 1, 0, 0, 0, DateTimeKind.Unspecified);
                holidays.Add(holiday);

                // Independencia
                holiday = new DateTime(year, 9, 7, 0, 0, 0, DateTimeKind.Unspecified);
                holidays.Add(holiday);

                // Nossa Sra Aparecida
                holiday = new DateTime(year, 10, 12, 0, 0, 0, DateTimeKind.Unspecified);
                holidays.Add(holiday);

                // Finados
                holiday = new DateTime(year, 11, 2, 0, 0, 0, DateTimeKind.Unspecified);
                holidays.Add(holiday);

                // Republica
                holiday = new DateTime(year, 11, 15, 0, 0, 0, DateTimeKind.Unspecified);
                holidays.Add(holiday);

                // Natal
                holiday = new DateTime(year, 12, 25, 0, 0, 0, DateTimeKind.Unspecified);
                holidays.Add(holiday);
            }

            return holidays;
        }


        /// <summary>
        ///     Retorna os parâmetros x e y (ajustes) necessários para calcular a data da pascoa dado o ano
        /// </summary>
        /// <param name="year">The year.</param>
        /// <param name="x">The x.</param>
        /// <param name="y">The y.</param>
        private static void GetXyForYear(int year, out int x, out int y)
        {
            x = 1;
            y = 1;

            if (year < 1582)
            {
                throw new ArgumentOutOfRangeException(nameof(year), year, "Ano mínimo é 1582");
            }

            if ((year < 1599))
            {
                x = 22;
                y = 2;
            }
            if (year is >= 1600 and < 1699)
            {
                x = 22;
                y = 2;
            }
            if (year is >= 1700 and < 1799)
            {
                x = 23;
                y = 3;
            }
            if (year is >= 1800 and < 1899)
            {
                x = 24;
                y = 4;
            }
            if (year is >= 1900 and < 1999)
            {
                x = 24;
                y = 5;
            }
            if (year is >= 2000 and < 2099)
            {
                x = 24;
                y = 5;
            }
            if (year is >= 2100 and < 2199)
            {
                x = 24;
                y = 6;
            }
            if (year is >= 2200 and < 2299)
            {
                x = 25;
                y = 7;
            }
            if (year > 2299)
            {
                throw new ArgumentOutOfRangeException(nameof(year), year, "Ano máximo é 2299");
            }
        }

    }
}