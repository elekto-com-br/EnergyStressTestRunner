using System;
using System.Collections.Generic;
using System.Linq;
using VoltElekto.Calendars;
using VoltElekto.Energy;

namespace VoltElekto.Risk;

public class RiskFactorServer
{
    private readonly ICurveServer _curveServer;
    private readonly ICalendar _calendar;

    public RiskFactorServer(ICurveServer curveServer, ICalendar calendar)
    {
        _curveServer = curveServer;
        _calendar = calendar;
    }

    public RiskFactor GetRiskFactor(string name, int relativeMonth, DateTime minDate, DateTime maxDate, int returnsPeriod)
    {
        var dates = _calendar.GetWorkDates(minDate, maxDate, DeltaTerminalDayAdjust.StartAndEndCollapsing).ToArray();

        var prices = new List<(DateTime date, double price, double returnOnPeriod)>(dates.Length);

        for (var i = returnsPeriod; i < dates.Length; i++)
        {
            var previousDate = dates[i - returnsPeriod];
            var currentDate = dates[i];

            var deliveryMonth = _calendar.GetActualMonthHead(currentDate, relativeMonth);
            var payDate = _calendar.AddWorkDays(deliveryMonth.AddMonths(1).AddDays(-1), 6);
                
            var currentCurve = _curveServer.GetCurve(currentDate);
            var previousCurve = _curveServer.GetCurve(previousDate);

            var currentValue = currentCurve.GetValue(payDate);
            var previousValue = previousCurve.GetValue(payDate);

            if (_calendar.GetWorkingMonthHead(currentDate, 0) == currentDate)
            {
                // O valor prévio vem da mercadoria seguinte
                var deliveryMonthOnMonthHead = _calendar.GetActualMonthHead(currentDate, relativeMonth + 1);
                var payDateMonthHead = _calendar.AddWorkDays(deliveryMonthOnMonthHead.AddMonths(1).AddDays(-1), 6);
                previousValue = previousCurve.GetValue(payDateMonthHead);
            }

            var returnOnPeriod = (currentValue - previousValue) / previousValue;
                
            prices.Add((currentDate, currentValue, returnOnPeriod));
        }
            
        return new RiskFactor(name, prices);
    }

}