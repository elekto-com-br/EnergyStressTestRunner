using System;
using System.Linq;
using VoltElekto.Calendars;

namespace VoltElekto.Energy.Margin
{
    public class MarginParameters
    {
        public string Name { get; set; }
        public VertexParameter[] Vertices { get; set; }

        public void Normalize()
        {
            Vertices = Vertices.OrderBy(v => v.ReferenceMonth).ToArray();
        }

        public double GetCoverageFactor(DateTime referenceDate, DateTime productDate)
        {
            var referenceMonth = referenceDate.GetSerialMonth();
            var productMonth = productDate.GetSerialMonth();
            var relativeMonth = productMonth - referenceMonth;

            if (relativeMonth < Vertices[0].ReferenceMonth)
            {
                relativeMonth = Vertices[0].ReferenceMonth;
            }

            if (relativeMonth > Vertices[Vertices.Length - 1].ReferenceMonth)
            {
                relativeMonth = Vertices[Vertices.Length - 1].ReferenceMonth;
            }

            return Vertices.First(v => v.ReferenceMonth == relativeMonth).Coverage;

        }
    }
}