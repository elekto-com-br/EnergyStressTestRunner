using System;

namespace VoltElekto.Energy
{
    /// <summary>
    /// Resultado de Stress
    /// </summary>
    public class StressResult
    {
        
        /// <summary>
        /// Valor 
        /// </summary>
        public double Value { get; set; }

        /// <summary>
        /// Valor da Ponta Fixa
        /// </summary>
        public double FixedValue { get; set; }

        /// <summary>
        /// Valor da Ponta Flutuante (a Energia) 
        /// </summary>
        public double EnergyValue { get; set; }

        public double ValueParallelPlus { get; set; }
        public double ValueParallelMinus { get; set; }
        public double ValueShortPlus { get; set; }
        public double ValueShortMinus { get; set; }
        public double ValueAscendent { get; set; }
        public double ValueDescendent { get; set; }

        public double StressParallelPlus => ValueParallelPlus - Value;
        public double StressParallelMinus => ValueParallelMinus - Value;
        public double StressShortPlus => ValueShortPlus - Value;
        public double StressShortMinus => ValueShortMinus - Value;
        public double StressAscendent => ValueAscendent - Value;
        public double StressDescendent => ValueDescendent - Value;

        public double ReferenceStress { get; set; }
        public string WorstStress { get; set; }

        public virtual void CalculateReferenceStress()
        {
            var min = StressParallelPlus;
            WorstStress = "P+";

            if (StressParallelMinus < min)
            {
                min = StressParallelMinus;
                WorstStress = "P-";
            }

            if (StressShortPlus < min)
            {
                min = StressShortPlus;
                WorstStress = "S+";
            }

            if (StressShortMinus < min)
            {
                min = StressShortMinus;
                WorstStress = "S-";
            }

            if (StressAscendent < min)
            {
                min = StressAscendent;
                WorstStress = "A";
            }

            if (StressDescendent < min)
            {
                min = StressDescendent;
                WorstStress = "D";
            }

            // A não ser operando próximo dos limites, sempre haverá um cenário perdedor
            if (min > 0)
            {
                min = 0;
            }

            ReferenceStress = Math.Abs(min);

        }
    }
}