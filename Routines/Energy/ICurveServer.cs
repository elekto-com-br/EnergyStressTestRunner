using System;

namespace VoltElekto.Energy
{
    public interface ICurveServer
    {
        /// <summary>
        /// Obtém a curva numa data
        /// </summary>
        ICurve GetCurve(DateTime referenceDate);
    }
}