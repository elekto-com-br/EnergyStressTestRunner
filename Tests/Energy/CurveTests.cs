using System;
using System.Collections.Generic;
using NUnit.Framework;
using VoltElekto.Calendars;

namespace VoltElekto.Energy
{

    [TestFixture]
    public class CurveTests
    {
        [Test]
        public void BasicTest()
        {
            var calendar = PerpetualBrazilianCalendarProvider.GetCalendar();
            var referenceDate = new DateTime(2021, 09, 17);

            var list = new List<(DateTime date, double price)>
            {
                (referenceDate, 1.0),
                (calendar.AddWorkDays(referenceDate, 1), 2.0),
                (calendar.AddWorkDays(referenceDate, 21), 3.0),
                (calendar.AddWorkDays(referenceDate, 42), 3.0),
                (calendar.AddWorkDays(referenceDate, 63), 4.0),
                (calendar.AddWorkDays(referenceDate, 84), 5.0),
            };

            var curve = new Curve(referenceDate, calendar, list);

            Assert.AreEqual(referenceDate, curve.ReferenceDate);

            // Exatos
            Assert.AreEqual(1.0, curve.GetValue(referenceDate));
            Assert.AreEqual(2.0, curve.GetValue(calendar.AddWorkDays(referenceDate, 1)));
            Assert.AreEqual(3.0, curve.GetValue(calendar.AddWorkDays(referenceDate, 21)));
            Assert.AreEqual(3.0, curve.GetValue(calendar.AddWorkDays(referenceDate, 42)));
            Assert.AreEqual(4.0, curve.GetValue(calendar.AddWorkDays(referenceDate, 63)));
            Assert.AreEqual(5.0, curve.GetValue(calendar.AddWorkDays(referenceDate, 84)));
            
            // Extrapolado para o futuro
            Assert.AreEqual(5.0, curve.GetValue(calendar.AddWorkDays(referenceDate, 85)));
            Assert.AreEqual(5.0, curve.GetValue(calendar.AddWorkDays(referenceDate, 252)));

            // Interpolado na parte flat
            Assert.AreEqual(3.0, curve.GetValue(calendar.AddWorkDays(referenceDate, 21)));
            Assert.AreEqual(3.0, curve.GetValue(calendar.AddWorkDays(referenceDate, 22)));
            Assert.AreEqual(3.0, curve.GetValue(calendar.AddWorkDays(referenceDate, 30)));
            Assert.AreEqual(3.0, curve.GetValue(calendar.AddWorkDays(referenceDate, 41)));
            Assert.AreEqual(3.0, curve.GetValue(calendar.AddWorkDays(referenceDate, 42)));

            // Interpolado no 1º segmento
            Assert.AreEqual(2.45, curve.GetValue(calendar.AddWorkDays(referenceDate, 10)), 1e-10);

            // Interpolado no último segmento
            Assert.AreEqual(4.47619047619048, curve.GetValue(calendar.AddWorkDays(referenceDate, 73)), 1e-10);
        }

    }
}
