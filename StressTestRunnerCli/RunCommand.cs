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
using Newtonsoft.Json;
using VoltElekto.Calendars;
using VoltElekto.Collections.Generic;
using VoltElekto.Energy;
using VoltElekto.Energy.Margin;
using VoltElekto.Market;

namespace VoltElekto
{
    [Command("Run")]
    [PublicAPI]
    public class RunCommand : ICommand
    {
        [CommandParameter(0, Name = "Arquivo de Posições", Description = "Arquivo excel ou texto com as posições chave.", IsRequired = true)]
        public string PositionsFile { get; set; }

        [CommandOption("Stress", Description = "Stress Paralelo")]
        public double Stress { get; set; } = 47.0;

        [CommandOption("StressShort", Description = "Stress de Curto Prazo")]
        public double? StressShort { get; set; }

        [CommandOption("StressLong", Description = "Stress de Longo Prazo")]
        public double? StressLong { get; set; }

        [CommandOption("NetWorth", 'n', Description = "Patrimônio de Referência.")]
        public double NetWorth { get; set; } = 10_000_000.0;

        [CommandOption("CurveFile", 'c', Description = "Arquivo texto com as curvas de energia.")]
        public string CurveFile { get; set; }

        [CommandOption("HolidaysFile", 'h', Description = "Arquivo texto com os feriados.")]
        public string HolidaysFile { get; set; }

        [CommandOption("MarginsFile", 'm', Description = "Arquivo json com os parâmetros de margem/garantia. ")]
        public string MarginsFile { get; set; }


        public ValueTask ExecuteAsync(IConsole console)
        {
            if (NetWorth <= 1)
            {
                throw new CommandException("Patrimônio de Referência deve ser de pelo menos 1.");
            }

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

            // Parâmetros de Stress
            if (Stress <= 0.0)
            {
                throw new CommandException("O stress deve ser positivo !");
            }

            if (StressShort == null) StressShort = Stress;
            if (StressLong == null) StressLong = Stress;

            if (StressShort <= 0.0)
            {
                throw new CommandException("O stress curto deve ser positivo !");
            }

            if (StressLong <= 0.0)
            {
                throw new CommandException("O stress longo deve ser positivo !");
            }

            var stressParameters = new StressParameters
            {
                StressParallel = Stress / 100.0,
                StressShort = StressShort.Value / 100.0,
                StressLong = StressLong.Value / 100.0,
            };
            console.Output.WriteLine($"Stress Paralelo:    {stressParameters.StressParallel:P0}");
            console.Output.WriteLine($"Stress Curto Prazo: {stressParameters.StressShort:P0}");
            console.Output.WriteLine($"Stress Longo Prazo: {stressParameters.StressLong:P0}");

            // Parâmetros de Garantias/Margem
            MarginParameters marginParameters = null;
            if (!string.IsNullOrWhiteSpace(MarginsFile))
            {
                if (!File.Exists(MarginsFile))
                {
                    throw new CommandException($"O arquivo de margens '{MarginsFile}' não existe!");
                }

                var content = File.ReadAllText(MarginsFile, Encoding.UTF8);
                marginParameters = JsonConvert.DeserializeObject<MarginParameters>(content);

                if (marginParameters == null)
                {
                    throw new CommandException($"O arquivo de margens '{MarginsFile}' é inválido.");
                }

                marginParameters.Normalize();
                
                console.Output.WriteLine($"Parâmetros de Garantia/Margem '{marginParameters.Name}':");
                foreach (var v in marginParameters.Vertices)
                {
                    console.Output.WriteLine($"  M{v.ReferenceMonth}: {v.Coverage:P0}");
                }
            }

            const CalculationMode mode = CalculationMode.PositionInterpolated;
            console.Output.WriteLine($"Modo de Cálculo: {mode.GetDescription()}");

            // Lê as posições chave
            if (!File.Exists(PositionsFile))
            {
                throw new CommandException($"O arquivo de posições '{PositionsFile}' não existe!");
            }

            IPositionsServer positionsServer;
            if (PositionsFile.ToLowerInvariant().EndsWith(".txt"))
            {
                positionsServer = new PositionsServerFromText(PositionsFile, calendar);
            }
            else if (PositionsFile.ToLowerInvariant().EndsWith(".xlsx"))
            {
                positionsServer = new PositionsServerFromExcel(PositionsFile, calendar);
            }
            else
            {
                throw new CommandException("O arquivo de posições deve ser um .txt ou .xlsx!");
            }

            var positionsOrTrades = new List<EnergyPosition>();
            positionsOrTrades.AddRange(positionsServer.GetPositions());

            console.Output.WriteLine($"{positionsOrTrades.Count:N0} posições de referência relevantes lidos.");

            var calculator = new Calculator(calendar, curveServer);

            var allPositions = new List<EnergyPosition>();
            List<EnergyPosition> allTrades;
            switch (mode)
            {
                case CalculationMode.PositionInterpolated:
                    allPositions.AddRange(calculator.GetInterpolatedAllPositions(positionsOrTrades, out allTrades));
                    break;
                default:
                    throw new ArgumentOutOfRangeException(nameof(mode), mode, "Modo não suportado");
            }

            var minDate = allPositions.Min(p => p.ReferenceDate);
            var maxDate = allPositions.Max(p => p.ReferenceDate);
            var numDates = allPositions.Select(p => p.ReferenceDate).Distinct().Count();

            console.Output.WriteLine($"{allPositions.Count:N0} posições entre {minDate:yyyy-MM-dd} e {maxDate:yyyy-MM-dd} ({numDates:N0} datas) serão calculadas...");

            #region Dump dos Arquivos de Prova

            var path = Path.GetDirectoryName(PositionsFile) ?? string.Empty;
            var baseName = Path.GetFileNameWithoutExtension(PositionsFile);

            var sb = new StringBuilder();

            // Posições de Referência
            sb.Clear();
            sb.AppendLine("ReferenceDate\tProductDate\tAmount");
            foreach (var p in positionsOrTrades)
            {
                sb.AppendLine($"{p.ReferenceDate:yyyy-MM-dd}\t{p.StartMonth:yyyy-MM-dd}\t{p.Amount:G}");
            }
            File.WriteAllText(Path.Combine(path, $"{baseName}.RefPos.txt"), sb.ToString(), Encoding.UTF8);

            // Todas as Posições
            sb.Clear();
            sb.AppendLine("ReferenceDate\tProductDate\tAmount");
            foreach (var p in allPositions)
            {
                sb.AppendLine($"{p.ReferenceDate:yyyy-MM-dd}\t{p.StartMonth:yyyy-MM-dd}\t{p.Amount*p.BuySell.GetSignal():G}");
            }
            File.WriteAllText(Path.Combine(path, $"{baseName}.AllPos.txt"), sb.ToString(), Encoding.UTF8);

            // Todos os Trades
            sb.Clear();
            sb.AppendLine("ReferenceDate\tProductDate\tBuySell\tAmount\tTradePrice\tExpireDate\tTag");
            foreach (var p in allTrades)
            {
                sb.AppendLine($"{p.ReferenceDate:yyyy-MM-dd}\t{p.StartMonth:yyyy-MM-dd}\t{p.BuySell}\t{p.Amount:G}\t{p.TradePrice:G}\t{p.PayDate:yyyy-MM-dd}\t{p.Tag}");
            }
            File.WriteAllText(Path.Combine(path, $"{baseName}.Trades.txt"), sb.ToString(), Encoding.UTF8);

            // Todos do Estoque (o que entra no RiskSystem)
            sb.Clear();
            sb.AppendLine("Source\tReferenceDate\tProductDate\tBuySell\tAmount\tTradeDate\tTradePrice\tExpireDate\tTag");
            foreach (var p in allPositions)
            {
                sb.AppendLine($"{baseName}\t{p.ReferenceDate:yyyy-MM-dd}\t{p.StartMonth:yyyy-MM-dd}\t{(int)p.BuySell.GetSignal()}\t{p.Amount:G}\t{p.TradeDate:yyyy-MM-dd}\t{p.TradePrice:G}\t{p.PayDate:yyyy-MM-dd}\t{p.Tag}");
            }
            File.WriteAllText(Path.Combine(path, $"{baseName}.RiskSystemPos.txt"), sb.ToString(), Encoding.UTF8);

            console.Output.WriteLine($"Arquivos de Prova escritos no diretório: {path}");

            #endregion

            var stresses = calculator.CalculateStress(allPositions, stressParameters, marginParameters);

            #region Dump do Stress em cada data

            sb.Clear();
            sb.AppendLine("ReferenceDate\tValue\tFixedValue\tEnergyValue\tStressParallelPlus\tStressParallelMinus\tStressShortPlus\tStressShortMinus\tStressAscendent\tStressDescendent\tReferenceStress\tWorstStress");
            foreach (var p in stresses)
            {
                sb.AppendLine($"{p.ReferenceDate:yyyy-MM-dd}\t{p.Value:G}\t{p.FixedValue:G}\t{p.EnergyValue:G}\t{p.StressParallelPlus:G}\t{p.StressParallelMinus:G}\t{p.StressShortPlus:G}\t{p.StressShortMinus:G}\t{p.StressAscendent:G}\t{p.StressDescendent:G}\t{p.ReferenceStress:G}\t{p.WorstStress}");
            }

            var stressFileName = Path.Combine(path, $"{baseName}.PortfolioStress.txt");
            File.WriteAllText(stressFileName, sb.ToString(), Encoding.UTF8);

            console.Output.WriteLine($"Arquivo texto com os stresses escrito em '{stressFileName}'. Formatando Excel...");

            #endregion

            var templateFile = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Templates", "Report.xlsx");
            if (!File.Exists(templateFile))
            {
                throw new FileNotFoundException("Arquivo de template não encontrado", templateFile);
            }

            // Vai sobrescrever, ou nem vai fazer
            var reportFile = Path.Combine(path, $"{baseName}.Report.xlsx");
            calculator.WriteReport(templateFile, reportFile, stresses, allPositions, allTrades, NetWorth, stressParameters, PositionsFile, MarginsFile);

            console.Output.WriteLine($"Relatório salvo em '{reportFile}'.");

            return default;
        }
    }
}