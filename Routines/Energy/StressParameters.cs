using System;
using VoltElekto.Calendars;

namespace VoltElekto.Energy
{
    /// <summary>
    /// Parâmetros de Stress
    /// </summary>
    public class StressParameters
    {
        /// <summary>
        /// Stress no Curto Prazo
        /// </summary>
        /// <remarks>
        /// Valor Default calculado com horizonte de 4 anos em Jun/2021, no prazo M3
        /// </remarks>
        public double StressShort { get; set; } = 0.53;

        /// <summary>
        /// Stress no Médio Prazo
        /// </summary>
        /// <remarks>
        /// Valor Default calculado com horizonte de 4 anos em Jun/2021, no prazo M1
        /// </remarks>
        public double StressParallel { get; set; } = 0.47;

        /// <summary>
        /// Stress no Longo Prazo
        /// </summary>
        /// <remarks>
        /// Valor Default calculado com horizonte de 4 anos em Jun/2021, no prazo M5
        /// </remarks>
        public double StressLong { get; set; } = 0.48;

        /// <summary>
        /// Fator de Tempo
        /// </summary>
        /// <remarks>
        /// Relacionado à janela de análise, o valor padrão corresponde para até M6, estável para um dado horizonte
        /// </remarks>
        public double TimeFactor { get; set; } = 2.2;

        /// <summary>
        /// Fator do Ascendente A
        /// </summary>
        /// <remarks>
        /// Relacionado à janela de análise, o valor padrão corresponde para até M6, estável para um dado horizonte
        /// </remarks>
        public double SteppenerA { get; set; } = -0.80;

        /// <summary>
        /// Fator do Ascendente B
        /// </summary>
        /// <remarks>
        /// Relacionado à janela de análise, o valor padrão corresponde para até M6, estável para um dado horizonte
        /// </remarks>
        public double SteppenerB { get; set; } = +0.80;

        /// <summary>
        /// Fator do Descendente A
        /// </summary>
        /// <remarks>
        /// Relacionado à janela de análise, o valor padrão corresponde para até M6, estável para um dado horizonte
        /// </remarks>
        public double FlattenerA { get; set; } = +0.80;

        /// <summary>
        /// Fator do Descendente B
        /// </summary>
        /// <remarks>
        /// Relacionado à janela de análise, o valor padrão corresponde para até M6, estável para um dado horizonte
        /// </remarks>
        public double FlattenerB { get; set; } = -0.80;

        public (double zero, double parallelPlus, double parallelMinus, double shortPlus, double shortMinus, double ascendent, double descendent) GetValue(ICalendar calendar, DateTime referenceDate, DateTime date, double normalValue, PldLimits limits)
        {
            var maturity = calendar.GetDeltaWorkDays(referenceDate, date);
            if (maturity < 0)
            {
                throw new ArgumentOutOfRangeException(nameof(date), date, $"A data deve ser ≥ {referenceDate:yyyy-MM-dd}");
            }

            var price = normalValue;

            if (maturity == 0)
            {
                return (price, price, price, price, price, price, price);
            }
            
            var t = maturity / 252.0;

            // paralelo+
            var pp = price * (1.0 + StressParallel);
            pp = limits.RestrictToLimits(date, pp);

            // paralelo-
            var pm = price * (1.0 - StressParallel);
            pm = limits.RestrictToLimits(date, pm);

            // short
            var sh = StressShort * Math.Exp(-t * TimeFactor);

            // short+
            var sp = price * (1.0 + sh);
            sp = limits.RestrictToLimits(date, sp);

            // short-
            var sm = price * (1.0 - sh);
            sm = limits.RestrictToLimits(date, sm);

            // descent (Flattener)
            sh = FlattenerA * Math.Abs(StressShort * Math.Exp(-t * TimeFactor)) + FlattenerB * Math.Abs(StressLong * (1.0 - Math.Exp(-t * TimeFactor)));
            var d = price * (1.0 + sh);
            d = limits.RestrictToLimits(date, d);

            // ascend (Steppener)
            sh = SteppenerA  * Math.Abs(StressShort * Math.Exp(-t * TimeFactor)) + SteppenerB * Math.Abs(StressLong  * (1.0 - Math.Exp(-t * TimeFactor)));
            var a = price * (1.0 + sh);
            a = limits.RestrictToLimits(date, a);

            return (price, pp, pm, sp, sm, a, d);

        }

    }
}