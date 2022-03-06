

namespace VoltElekto.Energy.Margin
{
    public class VertexParameter
    {
        public VertexParameter()
        {
        }

        public VertexParameter(int referenceMonth, double coverage)
        {
            ReferenceMonth = referenceMonth;
            Coverage = coverage;
        }

        public int ReferenceMonth { get; set; }

        public double Coverage { get; set; }
    }
}

