using System;

namespace VoltElekto.Energy
{
    public class EnergyPositionResult : StressResult
    {
        public EnergyPosition Position { get; }

        public EnergyPositionResult(EnergyPosition position)
        {
            Position = position;
        }

        public DateTime ReferenceDate => Position.ReferenceDate;

        public void CalculateResults((double zero, double parallelPlus, double parallelMinus, double shortPlus, double shortMinus, double ascendent, double descendent) price)
        {
            var volume = Position.GetVolume();
            
            var energyValue = volume * price.zero;
            var fixedValue = -volume * Position.TradePrice;
            var value = energyValue + fixedValue;

            // Valor Normal Net, do Dinheiro e da Energia
            Value = value;
            FixedValue = fixedValue;
            EnergyValue = energyValue;

            // paralelo+
            energyValue = volume * price.parallelPlus;
            value = energyValue + fixedValue;
            ValueParallelPlus += value;

            // paralelo-
            energyValue = volume * price.parallelMinus;
            value = energyValue + fixedValue;
            ValueParallelMinus += value;

            // short+
            energyValue = volume * price.shortPlus;
            value = energyValue + fixedValue;
            ValueShortPlus += value;

            // short-
            energyValue = volume * price.shortMinus;
            value = energyValue + fixedValue;
            ValueShortMinus += value;

            // ascend
            energyValue = volume * price.ascendent;
            value = energyValue + fixedValue;
            ValueAscendent += value;

            // descend
            energyValue = volume * price.descendent;
            value = energyValue + fixedValue;
            ValueDescendent += value;

            CalculateReferenceStress();
        }
    }
}