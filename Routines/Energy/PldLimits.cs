using System;
using System.Collections.Generic;
using System.Linq;

namespace VoltElekto.Energy
{
    /// <summary>
    /// Limites de PLD
    /// </summary>
    /// <remarks>
    /// Hardcoded
    /// </remarks>
    public class PldLimits
    {
        private readonly Dictionary<int, (double min, double max)> _pldLimits = new()
        {
            { 2016, (30.25, 422.56) },
            { 2017, (33.68, 533.82) },
            { 2018, (40.16, 505.18) },
            { 2019, (42.35, 513.89) },
            { 2020, (39.68, 559.75) },
            { 2021, (49.77, 583.88) }
        };

        public double RestrictToLimits(DateTime date, double value)
        {
            var year = date.Year;
            if (!_pldLimits.TryGetValue(year, out var limits))
            {
                var max = _pldLimits.Keys.Max();
                if (year > max)
                {
                    limits = _pldLimits[max];
                }
                else
                {
                    var min = _pldLimits.Keys.Min();
                    if (year < min)
                    {
                        limits = _pldLimits[min];
                    }
                    else
                    {
                        throw new ApplicationException($"Não existe limite de PLD configurado para {year}!");
                    }
                }
            }

            if (limits.min <= value && value <= limits.max)
            {
                // dentro!
                return value;
            }

            if (value > limits.max)
            {
                // no teto
                return limits.max;
            }

            if (value < limits.min)
            {
                // no piso
                return limits.min;
            }

            throw new ApplicationException("Erro de fluxo!");
        }
    }
}