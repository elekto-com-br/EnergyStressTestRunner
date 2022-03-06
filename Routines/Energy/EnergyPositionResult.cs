using System;
using VoltElekto.Energy.Margin;

namespace VoltElekto.Energy
{
    /// <summary>
    /// Preços Estressados
    /// </summary>
    public class StressedPrice
    {
        
        public double Zero { get; set; }

        public double ParallelPlus { get; set; }
        
        public double ParallelMinus { get; set; }
        
        public double ShortPlus { get; set; }
        
        public double ShortMinus { get; set; }

        public double Ascendent { get; set; }
        
        public double Descendent { get; set; }

    }

    public class EnergyPositionResult : StressResult
    {
        public EnergyPosition Position { get; }

        public EnergyPositionResult(EnergyPosition position)
        {
            Position = position;
        }

        public DateTime ReferenceDate => Position.ReferenceDate;

        /// <summary>
        /// Os preços usados no cálculo
        /// </summary>
        public StressedPrice Price { get; set; }

        /// <summary>
        /// Cenário usado para calcular a margem
        /// </summary>
        public string MarginScenario { get; set; }

        /// <summary>
        /// Margem Requerida
        /// </summary>
        public double MarginRequired { get; set; }

        /// <summary>
        /// Valor Base do cálculo da Margem
        /// </summary>
        public double MarginBase { get; set; }

        public void CalculateResults((double zero, double parallelPlus, double parallelMinus, double shortPlus, double shortMinus, double ascendent, double descendent) price)
        {
            // A ser usado posteriormente
            Price = new StressedPrice
            {
                Zero = price.zero,
                ParallelPlus = price.parallelPlus,
                ParallelMinus = price.parallelMinus,
                ShortPlus = price.shortPlus,
                ShortMinus = price.shortMinus,
                Ascendent = price.ascendent,
                Descendent = price.descendent
            };

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

        public void CalculateMargin(string worstScenario, MarginParameters marginParameters)
        {
            MarginScenario = worstScenario;

            MarginBase = worstScenario switch
            {
                Scenarios.ParallelPlus => StressParallelPlus,
                Scenarios.ParallelMinus => StressParallelMinus,
                Scenarios.ShortPlus => StressShortPlus,
                Scenarios.ShortMinus => StressShortMinus,
                Scenarios.Ascendent => StressAscendent,
                Scenarios.Descendent => StressDescendent,
                _ => throw new ArgumentOutOfRangeException(nameof(worstScenario), worstScenario, "Cenário não reconhecido")
            };

            // Só interessa se for um cenário onde realmente há perda
            MarginBase = Math.Min(MarginBase, 0.0);
            
            // Tornando o valor positivo para evitar confusão posterior
            MarginBase = Math.Abs(MarginBase);

            var coverage = marginParameters.GetCoverageFactor(Position.ReferenceDate, Position.StartMonth);
            MarginRequired = MarginBase * coverage;
        }
    }
}