using OfficeOpenXml.FormulaParsing.Excel.Functions.Math;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace VoltElekto.Risk
{
    public class RiskFactor
    {
        public RiskFactor(string name, List<(DateTime date, double price, double returnOnPeriod)> prices)
        {
            Name = name;
            Prices = prices;
        }

        public List<(DateTime date, double price, double returnOnPeriod)> Prices { get; }

        public string Name { get; }

        /// <summary>
        /// Devolve a correlação entre dois fatores de risco
        /// </summary>
        public List<(DateTime date, double correlation)> GetCorrelationsWith(RiskFactor riskFactorB, double lambda = 0.95, double lambdaCutOff = 0.01)
        {
            // A ideia é calcular quando um retorno terá peso inferior ao parâmetro de insignificância
            var volatilityWindow = (int)(Math.Ceiling(Math.Log(lambdaCutOff) / Math.Log(lambda)));

            var correlations = new List<(DateTime date, double correlation)>(Prices.Count);

            foreach (var (date, price, returnOnPeriod) in Prices)
            {
                // Os retornos, do mais recente para o mais antigo, dentro da janela de volatilidade
                var returnsA = Prices.Where(p => p.date <= date).OrderByDescending(p => p.date).Take(volatilityWindow).ToArray();
                var returnsB = riskFactorB.Prices.Where(p => p.date <= date).OrderByDescending(p => p.date).Take(volatilityWindow).ToArray();
                if (returnsA.Length < volatilityWindow || returnsB.Length < volatilityWindow)
                {
                    // Não há retornos suficientes para calcular a correlação
                    continue;
                }
                var averageA = returnsA.Select(p => p.returnOnPeriod).Average();
                var averageB = returnsB.Select(p => p.returnOnPeriod).Average();
                
                var weight = 1.0;
                double sumX = 0.0, sumY = 0.0, sumXy = 0.0;

                for (var i = 0; i < returnsA.Length; i++)
                {
                    var dx = returnsA[i].returnOnPeriod - averageA;
                    sumX += (dx*dx*weight);

                    var dy = returnsB[i].returnOnPeriod - averageB;
                    sumY += (dy*dy*weight);
                    
                    sumXy += (dx*dy*weight);
                    weight *= lambda;
                }

                var correlation = sumXy/Math.Sqrt(sumX*sumY);
                correlations.Add((date, correlation));
            }

            return correlations;
        }

        public List<(DateTime date, double price, double returnOnPeriod, double volatility)> GetVolatility(double lambda = 0.95, double lambdaCutOff = 0.01)
        {
            // A ideia é calcular quando um retorno terá peso inferior ao parâmetro de insignificância
            var volatilityWindow = (int)(Math.Ceiling(Math.Log(lambdaCutOff) / Math.Log(lambda)));

            var listVolatility = new List<(DateTime date, double price, double returnOnPeriod, double volatility)>(Prices.Count);

            foreach (var (date, price, returnOnPeriod) in Prices)
            {
                // Os retornos, do mais recente para o mais antigo, dentro da janela de volatilidade
                var returns = Prices.Where(p => p.date <= date).OrderByDescending(p => p.date).Take(volatilityWindow).ToArray();
                if (returns.Length < volatilityWindow)
                {
                    // Não há retornos suficientes para calcular a volatilidade
                    continue;
                }

                var average = returns.Select(p => p.returnOnPeriod).Average();
                var sum = 0.0;
                var weight = 1.0;
                var sumWeights = 0.0;
                foreach (var t in returns)
                {
                    var termo = t.returnOnPeriod - average;
                    sum += (termo*termo*weight);
                    sumWeights += weight;
                    weight *= lambda;
                }

                var volatility = Math.Sqrt(sum / sumWeights);

                listVolatility.Add((date, price, returnOnPeriod, volatility));
            }

            return listVolatility;
        }

        public (double lowerPercentile, double upperPercentile) GetStressCut(double stressCut)
        {
            var returns = Prices.Select(p => p.returnOnPeriod).OrderBy(r => r).ToArray();
            var lowerPercentile = Percentile(returns, stressCut);
            var upperPercentile = Percentile(returns, 1.0 - stressCut);

            return (lowerPercentile, upperPercentile);
        }

        /// <summary>
        /// Calcula os percentis de um conjunto de retornos interpolando se necessário
        /// </summary>
        private static double Percentile(double[] returns, double percentile)
        {
            // calculates the percentile of a sorted array
            var n = returns.Length;
            var h = (n - 1) * percentile + 1;
            var hFloor = Math.Floor(h);
            var hCeiling = Math.Ceiling(h);
            var hDiff = h - hFloor;
            if (Math.Abs(hFloor - hCeiling) < double.Epsilon)
            {
                return returns[(int)hFloor - 1];
            }
            var iFloor = (int)hFloor - 1;
            var iCeiling = (int)hCeiling - 1;
            return returns[iFloor] * (1 - hDiff) + returns[iCeiling] * hDiff;
        }
    }
}
