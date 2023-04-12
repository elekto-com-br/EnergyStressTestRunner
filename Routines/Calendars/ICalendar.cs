using System;
using System.Collections.Generic;

namespace VoltElekto.Calendars
{
    /// <summary>
    /// O que um calendário financeiro deve implementar
    /// </summary>
    public interface ICalendar
    {
        /// <summary>
        /// Nome do calendário
        /// </summary>
        string Name { get; }

        /// <summary>
        /// Descrição do calendário
        /// </summary>
        string Description { get; }

        /// <summary>
        /// Maior data que o calendário alcança
        /// </summary>
        DateTime MaxDate { get; }

        /// <summary>
        /// Menor data que o calendário alcança
        /// </summary>
        DateTime MinDate { get; }

        /// <summary>
        /// Os feriados todos
        /// </summary>
        IEnumerable<DateTime> Holidays { get; }

        /// <summary>
        /// Os dias não uteis da semana (normalmente apenas sabado e domingo)
        /// </summary>
        IEnumerable<DayOfWeek> NonWorkWeekDays { get; }

        /// <summary>
        /// Adiciona dias úteis
        /// </summary>
        /// <param name="date">The date.</param>
        /// <param name="workDays">The work days.</param>
        /// <param name="finalDateAdjust">The final date adjust.</param>
        /// <returns></returns>
        DateTime AddWorkDays(DateTime date, int workDays, FinalDateAdjust finalDateAdjust = FinalDateAdjust.Following);

        /// <summary>
        /// Numero de dias corridos entre duas datas
        /// </summary>
        /// <param name="ini">The ini.</param>
        /// <param name="end">The end.</param>
        /// <param name="adjust">The adjust.</param>
        /// <returns></returns>
        int GetDeltaActualDays(DateTime ini, DateTime end, DeltaTerminalDayAdjust adjust = DeltaTerminalDayAdjust.Financial);

        /// <summary>
        /// Numero de dias úteis entre duas datas
        /// </summary>
        /// <param name="ini">The ini.</param>
        /// <param name="end">The end.</param>
        /// <param name="adjust">The adjust.</param>
        /// <returns></returns>
        int GetDeltaWorkDays(DateTime ini, DateTime end, DeltaTerminalDayAdjust adjust = DeltaTerminalDayAdjust.Financial);

        /// <summary>
        /// Caso a data seja útil retorna a mesma data, caso não seja, retorna o próximo dia útil
        /// </summary>
        /// <param name="date">The date.</param>
        /// <returns></returns>
        DateTime GetNextOrSameWorkday(DateTime date);

        /// <summary>
        /// Caso a data seja útil retorna a mesma data, caso não seja, retorna o dia útil anterior
        /// </summary>
        /// <param name="date">The date.</param>
        /// <returns></returns>
        DateTime GetPrevOrSameWorkday(DateTime date);

        /// <summary>
        /// Retorna o dia útil anterior
        /// </summary>
        /// <param name="date">The date.</param>
        /// <returns></returns>
        DateTime GetPrevWorkday(DateTime date);

        /// <summary>
        /// Retorna somente os dias uteis dentro do intervalo
        /// </summary>
        /// <param name="ini">The ini.</param>
        /// <param name="end">The end.</param>
        /// <param name="deltaTerminalDayAdjust">The delta terminal day adjust.</param>
        /// <returns></returns>
        IEnumerable<DateTime> GetWorkDates(DateTime ini, DateTime end, DeltaTerminalDayAdjust deltaTerminalDayAdjust = DeltaTerminalDayAdjust.Financial);

        /// <summary>
        /// Testa se a data é um dia útil
        /// </summary>
        /// <param name="date">The date.</param>
        /// <returns>
        /// 	<c>true</c> if the specified date is workday; otherwise, <c>false</c>.
        /// </returns>
        bool IsWorkday(DateTime date);

        /// <summary>
        ///     Retorna o primeiro dia util do mês
        /// </summary>
        /// <param name="referenceDate">Data de referência</param>
        /// <param name="monthsAhead">Meses a adicionar (ou subtrair) da data de referencia.</param>
        /// <returns>O primeiro dia util do mês</returns>
        DateTime GetWorkingMonthHead(DateTime referenceDate, int monthsAhead);

        /// <summary>
        ///     Retorna o primeiro dia do mês
        /// </summary>
        /// <param name="referenceDate">Data de referência</param>
        /// <param name="monthsAhead">Meses a adicionar (ou subtrair) da data de referencia.</param>
        /// <returns>O primeiro dia do mês</returns>
        DateTime GetActualMonthHead(DateTime referenceDate, int monthsAhead);
    }
}