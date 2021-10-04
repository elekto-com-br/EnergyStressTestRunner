using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;

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
    }
}