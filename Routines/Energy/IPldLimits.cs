using System;

namespace VoltElekto.Energy
{
    public interface IPldLimits
    {
        /// <summary>
        /// Devolve um preço de PLD ajustado para os limites de preço de PLD
        /// </summary>
        public double RestrictToLimits(DateTime date, double value);
    }
}