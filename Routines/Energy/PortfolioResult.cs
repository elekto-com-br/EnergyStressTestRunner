using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using VoltElekto.Energy.Margin;

namespace VoltElekto.Energy
{
    /// <summary>
    /// Resultados de um Portfólio em uma data
    /// </summary>
    public class PortfolioResult : StressResult
    {
        private readonly List<EnergyPositionResult> _positions = new();

        /// <summary>
        /// Data de Referência
        /// </summary>
        public DateTime ReferenceDate { get; set; }

        /// <summary>
        /// Margem Requerida para o portfólio todo
        /// </summary>
        public double MarginRequired { get; set; }

        public void Add(EnergyPositionResult positionResult)
        {
            _positions.Add(positionResult);
        }

        public IEnumerable<EnergyPositionResult> Results => _positions.AsEnumerable();

        public override void CalculateReferenceStress()
        {
            foreach (var p in _positions)
            {
                Value += p.Value;
                FixedValue += p.FixedValue;
                EnergyValue += p.EnergyValue;

                ValueParallelPlus += p.ValueParallelPlus;
                ValueParallelMinus += p.ValueParallelMinus;

                ValueShortPlus += p.ValueShortPlus;
                ValueShortMinus += p.ValueShortMinus;

                ValueAscendent += p.ValueAscendent;
                ValueDescendent += p.ValueDescendent;
            }

            base.CalculateReferenceStress();
        }

        /// <summary>
        /// Calcula a margem necessária para o portfólio
        /// </summary>
        /// <remarks>
        /// É a soma das margens necessárias para cada posição, sob o pior cenário do portfólio como um todo
        /// </remarks>
        public void CalculateMargin(MarginParameters marginParameters)
        {
            var worstScenario = WorstStress;

            foreach (var p in _positions)
            {
                p.CalculateMargin(worstScenario, marginParameters);
            }

            MarginRequired = _positions.Sum(p => p.MarginRequired);

            // Só interessa se for um cenário onde realmente há perda
            MarginRequired = Math.Min(MarginRequired, 0.0);

            // Tornando o valor positivo para evitar confusão posterior
            MarginRequired = Math.Abs(MarginRequired);

            // A margem pode ser coberta (parcialmente) pelo próprio valor do portfolio
            MarginRequired -= Value;
            MarginRequired = Math.Max(MarginRequired, 0.0);

        }

        
    }
}