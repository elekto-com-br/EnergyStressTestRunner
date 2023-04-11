using System;
using System.Collections.Generic;
using System.Linq;

namespace VoltElekto.Energy;

public abstract class PldLimitsBase : IPldLimits
{
    protected Dictionary<int, (double min, double max)> PldLimits { get; set; }

    public double RestrictToLimits(DateTime date, double value)
    {
        var year = date.Year;
        if (!PldLimits.TryGetValue(year, out var limits))
        {
            var max = PldLimits.Keys.Max();
            if (year > max)
            {
                limits = PldLimits[max];
            }
            else
            {
                var min = PldLimits.Keys.Min();
                if (year < min)
                {
                    limits = PldLimits[min];
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