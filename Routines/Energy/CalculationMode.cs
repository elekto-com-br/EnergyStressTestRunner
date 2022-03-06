using System.ComponentModel;

namespace VoltElekto.Energy
{
    public enum CalculationMode
    {
        [Description("Posições Interpoladas")]
        PositionInterpolated = 0,

        [Description("Posições Como Informadas")]
        PositionAbsolute = 1,

        [Description("Negociações")]
        Trades = 2
    }
}