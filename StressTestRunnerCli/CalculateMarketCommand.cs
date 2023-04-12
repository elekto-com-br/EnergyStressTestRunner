using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using CliFx;
using CliFx.Attributes;
using CliFx.Exceptions;
using CliFx.Infrastructure;
using JetBrains.Annotations;
using VoltElekto.Calendars;
using VoltElekto.Energy;
using VoltElekto.Risk;

namespace VoltElekto
{
    [Command("CalculateMarket", Description = "Calcula os parâmetros de risco para VaR Paramétrico")]
    [PublicAPI]
    public class CalculateMarketCommand : ICommand
    {
        [CommandOption("CurveFile", 'c', Description = "Arquivo texto com as curvas de energia.")]
        public string CurveFile { get; set; }

        [CommandOption("HolidaysFile", 'h', Description = "Arquivo texto com os feriados.")]
        public string HolidaysFile { get; set; }

        [CommandOption("ReturnsPeriod", Description = "Período dos retornos calculados.")]
        public int ReturnsPeriod { get; set; } = 1;

        [CommandOption("MaxDeliveryMonth", Description = "Maior mês de entrega.")]
        public int MaxDeliveryMonth {get; set; } = 6;

        [CommandOption("Lambda", Description = "Decaimento Exponencial.")]
        public double Lambda { get; set; } = 0.95;

        [CommandOption("Irrelevance", Description = "Ponto de parada para a composição do decaimento exponencial.")]
        public double Irrelevance { get; set; } = 0.01;

        [CommandOption("MaxDate", Description = "Maior data para computar, sujeita ao que existir na curva.")]
        public DateTime? MaxDate { get; set; }

        [CommandOption("MinDate", Description = "Menor data para computar, sujeita ao que existir na curva.")]
        public DateTime? MinDate { get; set; } 

        [CommandOption("VolatilityFile", Description = "Arquivo texto onde escrever as volatilidades.")]
        public string OutputVolatilityFile { get; set; }

        [CommandOption("OutputCorrelationFile", Description = "Arquivo texto onde escrever as correlações.")]
        public string OutputCorrelationFile { get; set; }

        [CommandOption("StressCut", Description = "Percentil (em valor absoluto) para cortar o stress.")]
        public double StressCut { get; set; } = 0.01;

        [CommandOption("StressFile", Description = "Arquivo texto onde escrever os percentis de stress.")]
        public string OutputStressFile { get; set; }

        public ValueTask ExecuteAsync(IConsole console)
        {
            var currentDirectory = AppDomain.CurrentDomain.BaseDirectory;
            var dataDirectory = Path.Combine(currentDirectory, "Data");

            if (string.IsNullOrWhiteSpace(HolidaysFile))
            {
                // Tenta do diretório de dados

                if (!Directory.Exists(dataDirectory))
                {
                    throw new CommandException($"O diretório de dados '{dataDirectory}' não existe!");
                }

                HolidaysFile = Path.Combine(dataDirectory, "feriados.txt");
                if (!File.Exists(HolidaysFile))
                {
                    throw new CommandException($"O arquivo de feriados '{HolidaysFile}' não existe!");
                }
            }
            else
            {
                if (!File.Exists(HolidaysFile))
                {
                    throw new CommandException($"O arquivo de feriados '{HolidaysFile}' não existe!");
                }
            }

            var calendar = Calendar.BuildFromFile(HolidaysFile);
            console.Output.WriteLine($"Calendário tem {calendar.Holidays.Count():N0} feriados lidos do arquivo {HolidaysFile}.");

            if (string.IsNullOrWhiteSpace(CurveFile))
            {
                // Tenta do diretório de dados

                if (!Directory.Exists(dataDirectory))
                {
                    throw new CommandException($"O diretório de dados '{dataDirectory}' não existe!");
                }

                CurveFile = Path.Combine(dataDirectory, "Energia.txt");
                if (!File.Exists(CurveFile))
                {
                    throw new CommandException($"O arquivo de preços '{CurveFile}' não existe!");
                }
            }
            else
            {
                if (!File.Exists(CurveFile))
                {
                    throw new CommandException($"O arquivo de preços '{CurveFile}' não existe!");
                }
            }

            var curveServer = new CurveServerFromTextFile(CurveFile, calendar);
            console.Output.WriteLine($"Há curvas entre {curveServer.MinDate:yyyy-MM-dd} e {curveServer.MaxDate:yyyy-MM-dd} a partir do arquivo {CurveFile}.");

            if (MaxDate == null)
            {
                MaxDate = curveServer.MaxDate;
            }
            else
            {
                if (MaxDate > curveServer.MaxDate)
                {
                    MaxDate = curveServer.MaxDate;
                }
            }

            if (MinDate == null)
            {
                MinDate = curveServer.MinDate;
            }
            else
            {
                if (MinDate < curveServer.MinDate)
                {
                    MinDate = curveServer.MinDate;
                }
            }
            console.Output.WriteLine($"Serão calculados valores entre {MinDate:yyyy-MM-dd} e {MaxDate:yyyy-MM-dd}.");

            // Cria os fatores de risco
            var riskFactorServer = new RiskFactorServer(curveServer, calendar);

            var riskFactors = new RiskFactor[MaxDeliveryMonth+1];
            for (var i = 0; i <= MaxDeliveryMonth; ++i)
            {
                console.Output.WriteLine($"Construindo fator de risco para M{i}...");

                riskFactors[i] = riskFactorServer.GetRiskFactor($"M{i}", i, MinDate.Value, MaxDate.Value, ReturnsPeriod);
            }

            // Calcula as volatilidades

            if (OutputVolatilityFile == null)
            {
                OutputVolatilityFile = Path.Combine(currentDirectory, "Vols.txt");
            }

            var sb = new StringBuilder();
            sb.AppendLine("Data\tNome\tPreço\tRetorno\tVolatilidade");

            for (var i = 0; i <= MaxDeliveryMonth; ++i)
            {
                var riskFactor = riskFactors[i];
                console.Output.WriteLine($"Calculando volatilidades para {riskFactor.Name}...");

                var vs = riskFactor.GetVolatility(Lambda, Irrelevance);

                foreach (var (date, price, returnOnPeriod, volatility) in vs)
                {
                    sb.AppendLine($"{date:yyyy-MM-dd}\t{riskFactor.Name}\t{price}\t{returnOnPeriod}\t{volatility}");
                }
            }
            File.WriteAllText(OutputVolatilityFile, sb.ToString(), Encoding.UTF8);
            console.Output.WriteLine($"Volatilidades salvas em {OutputVolatilityFile}.");
            sb.Clear();

            // Calcula as correlações

            if (OutputCorrelationFile == null)
            {
                OutputCorrelationFile = Path.Combine(currentDirectory, "Correl.txt");
            }

            sb.AppendLine("Data\tNome A\tNome B\tCorrelação");

            for (var i = 0; i <= MaxDeliveryMonth; ++i)
            {
                var riskFactorA = riskFactors[i];

                for (var j = i + 1; j <= MaxDeliveryMonth; ++j)
                {
                    var riskFactorB = riskFactors[j];
                    console.Output.WriteLine($"Calculando correlação entre {riskFactorA.Name} e {riskFactorB.Name}...");
                    var correlations = riskFactorA.GetCorrelationsWith(riskFactorB, Lambda, Irrelevance);

                    foreach (var correlation in correlations)
                    {
                        sb.AppendLine($"{correlation.date:yyyy-MM-dd}\t{riskFactorA.Name}\t{riskFactorB.Name}\t{correlation.correlation}");
                    }
                }
            }

            File.WriteAllText(OutputCorrelationFile, sb.ToString(), Encoding.UTF8);
            console.Output.WriteLine($"Correlações salvas em {OutputVolatilityFile}.");
            sb.Clear();

            // Calcula o percentil dos retornos de cada fator de risco

            if (OutputStressFile == null)
            {
                OutputStressFile = Path.Combine(currentDirectory, "Stress.txt");
            }

            sb.AppendLine("Data\tPercentil\tLow\tHigh");

            for (var i = 0; i <= MaxDeliveryMonth; ++i)
            {
                var riskFactor = riskFactors[i];
                console.Output.WriteLine($"Calculando corte do stress para {riskFactor.Name}...");

                var (lowerPercentile, upperPercentile) = riskFactor.GetStressCut(StressCut);
                sb.AppendLine($"{riskFactor.Name}\t{StressCut}\t{lowerPercentile}\t{upperPercentile}");
            }

            File.WriteAllText(OutputStressFile, sb.ToString(), Encoding.UTF8);
            console.Output.WriteLine($"Stress salvo em {OutputStressFile}.");
            sb.Clear();

            return default;
        }
    }
}