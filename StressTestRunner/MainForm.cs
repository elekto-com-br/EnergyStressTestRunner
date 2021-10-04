using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;
using VoltElekto.Calendars;
using VoltElekto.Collections.Generic;
using VoltElekto.Energy;
using VoltElekto.Market;
using Calendar = VoltElekto.Calendars.Calendar;

namespace VoltElekto
{
    public partial class MainForm : Form
    {
        private ICalendar _calendar;
        private ICurveServer _curveServer;

        public MainForm()
        {
            InitializeComponent();
        }

        private void buttonOpenFile_Click(object sender, EventArgs e)
        {
            try
            {
                var res = openFileDialog.ShowDialog();
                if (res != DialogResult.OK)
                {
                    return;
                }

                var fileName = openFileDialog.FileName;
                textBoxFile.Text = fileName;

                AddStatusText($"Arquivo selecionado: {fileName}");
            }
            catch (Exception ex)
            {
                AddStatusText(ex.ToString());
                MessageBox.Show(ex.ToString(), "Erro", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        private void buttonExecute_Click(object sender, EventArgs e)
        {
            try
            {
                Cursor = Cursors.WaitCursor;
                
                var fileName = textBoxFile.Text;
                if (string.IsNullOrWhiteSpace(fileName))
                {
                    throw new ApplicationException("Arquivo não informado!");
                }

                if (!File.Exists(fileName))
                {
                    throw new FileNotFoundException("Arquivo não encontrado!", fileName);
                }
                AddStatusText($"Iniciando com o arquivo '{fileName}'...");

                if (!double.TryParse(textBoxStress.Text, out var stress))
                {
                    throw new ApplicationException("O parâmetro de stress deve ser informado.");
                }

                if (stress <= 0)
                {
                    throw new ApplicationException("O parâmetro de stress deve positivo.");
                }
                stress /= 100.0;
                var stressParameters = new StressParameters();

                // Flat simplifica cálculos e entendimento, e para prazos menores que um ano sequer faz diferença
                stressParameters.StressParallel = stressParameters.StressLong = stressParameters.StressShort = stress;

                if (!double.TryParse(textBoxNetworth.Text, out var capital))
                {
                    throw new ApplicationException("O Capital deve ser informado.");
                }

                var mode = ((EnumDescription<CalculationMode>)comboBoxMode.SelectedItem).Value;
                AddStatusText($"Modo de Cálculo: {mode.GetDescription()}...");

                var positionsServer = new PositionsServerFromExcel(fileName, _calendar);
                var positionsOrTrades = new List<EnergyPosition>();

                switch (mode)
                {
                    case CalculationMode.PositionInterpolated:
                    case CalculationMode.PositionAbsolute:
                        positionsOrTrades.AddRange(positionsServer.GetPositions());
                        break;
                    case CalculationMode.Trades:
                        positionsOrTrades.AddRange(positionsServer.GetTrades());
                        break;
                    default:
                        throw new ArgumentOutOfRangeException(nameof(mode), mode, "Modo não suportado");
                }

                if (!positionsOrTrades.Any())
                {
                    throw new ApplicationException("Nenhuma posição ou trade foi lida.");
                }

                AddStatusText($"{positionsOrTrades.Count:N0} posições ou trades relevantes lidos.");

                var calculator = new Calculator(_calendar, _curveServer);

                var allPositions = new List<EnergyPosition>();
                var allTrades = new List<EnergyPosition>();
                switch (mode)
                {
                    case CalculationMode.PositionInterpolated:
                        allPositions.AddRange(calculator.GetInterpolatedAllPositions(positionsOrTrades, out allTrades));
                        break;
                    case CalculationMode.PositionAbsolute:
                        allPositions.AddRange(calculator.GetAllPositions(positionsOrTrades));
                        break;
                    case CalculationMode.Trades:
                        allPositions.AddRange(calculator.GetAllPositionsFromTrades(positionsOrTrades));
                        break;
                    default:
                        throw new ArgumentOutOfRangeException(nameof(mode), mode, "Modo não suportado");
                }

                var minDate = allPositions.Min(p => p.ReferenceDate);
                var maxDate = allPositions.Max(p => p.ReferenceDate);
                var numDates = allPositions.Select(p => p.ReferenceDate).Distinct().Count();

                AddStatusText($"{allPositions.Count:N0} posições entre {minDate:yyyy-MM-dd} e {maxDate:yyyy-MM-dd} ({numDates:N0} datas) serão calculadas...");

                #region Dump dos Arquivos de Prova

                var path = Path.GetDirectoryName(fileName) ?? string.Empty;
                var baseName = Path.GetFileNameWithoutExtension(fileName);
	
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
                    sb.AppendLine($"{p.ReferenceDate:yyyy-MM-dd}\t{p.StartMonth:yyyy-MM-dd}\t{p.Amount:G}");
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

                AddStatusText($"Arquivos de Prova escritos no diretório: {path}");

                #endregion

                var stresses = calculator.CalculateStress(allPositions, stressParameters);

                #region Dump do Stress em cada data

                sb.Clear();
                sb.AppendLine("ReferenceDate\tValue\tFixedValue\tEnergyValue\tStressParallelPlus\tStressParallelMinus\tStressShortPlus\tStressShortMinus\tStressAscendent\tStressDescendent\tReferenceStress\tWorstStress");
                foreach (var p in stresses)
                {
                    sb.AppendLine($"{p.ReferenceDate:yyyy-MM-dd}\t{p.Value:G}\t{p.FixedValue:G}\t{p.EnergyValue:G}\t{p.StressParallelPlus:G}\t{p.StressParallelMinus:G}\t{p.StressShortPlus:G}\t{p.StressShortMinus:G}\t{p.StressAscendent:G}\t{p.StressDescendent:G}\t{p.ReferenceStress:G}\t{p.WorstStress:G}");
                }

                var stressFileName = Path.Combine(path, $"{baseName}.PortfolioStress.txt");
                File.WriteAllText(stressFileName, sb.ToString(), Encoding.UTF8);

                AddStatusText($"Arquivo texto com os stresses escrito em '{stressFileName}'. Formatando Excel...");

                #endregion

                var templateFile = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Templates", "Report.xlsx");
                if (!File.Exists(templateFile))
                {
                    throw new FileNotFoundException("Arquivo de template não encontrado", templateFile);
                }

                var reportFile = Path.Combine(path, $"{baseName}.Report.xlsx");
                var tryCount = 0;
                while (File.Exists(reportFile))
                {
                    reportFile = Path.Combine(path, $"{baseName}.Report.{tryCount:0000}.xlsx");
                    ++tryCount;
                }

                calculator.WriteReport(templateFile, reportFile, stresses, allPositions, allTrades, capital, stressParameters);

                AddStatusText($"Relatório salvo em '{reportFile}'.");

            }
            catch (FileNotFoundException ex)
            {
                var msg = string.IsNullOrWhiteSpace(ex.FileName) ? "Arquivo não encontrado." : $"Arquivo '{ex.FileName}' não encontrado.";
                AddStatusText(ex.ToString());
                MessageBox.Show(msg, "Erro", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
            catch (ApplicationException ex)
            {
                AddStatusText(ex.ToString());
                MessageBox.Show(ex.Message, "Erro", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
            catch (Exception ex)
            {
                AddStatusText(ex.ToString());
                MessageBox.Show(ex.ToString(), "Erro", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
            finally
            {
                Cursor = Cursors.Default;
            }
        }

        private enum CalculationMode
        {
            [Description("Posições Interpoladas")]
            PositionInterpolated = 0,

            [Description("Posições Como Informadas")]
            PositionAbsolute = 1,

            [Description("Negociações")]
            Trades = 2
        }

        
        private void AddStatusText(string text)
        {
            var current = textBoxStatus.Text;
            var newText = current;
            if (!string.IsNullOrWhiteSpace(current))
            {
                newText += Environment.NewLine;
            }

            newText += $"{DateTime.Now:HH:mm:ss}: " + text;
            textBoxStatus.Text = newText;
        }

        private void MainForm_Load(object sender, EventArgs e)
        {
            try
            {
                Cursor = Cursors.WaitCursor;

                // Localiza o arquivo de curvas para determinar limites das datas
                var currentDirectory = AppDomain.CurrentDomain.BaseDirectory;
                var dataDirectory = Path.Combine(currentDirectory, "Data");

                if (!Directory.Exists(dataDirectory))
                {
                    throw new FileNotFoundException($"O diretório de dados '{dataDirectory}' não existe!");
                }

                var holidayFile = Path.Combine(dataDirectory, "feriados.txt");
                if (!File.Exists(holidayFile))
                {
                    throw new FileNotFoundException($"O arquivo de feriados '{holidayFile}' não existe!");
                }
                
                var calendar = Calendar.BuildFromFile(holidayFile);
                AddStatusText($"Calendário tem {calendar.Holidays.Count():N0} feriados.");

                var curveFile = Path.Combine(dataDirectory, "Energia.txt");
                if (!File.Exists(holidayFile))
                {
                    throw new FileNotFoundException($"O arquivo com as curvas de energia '{curveFile}' não existe!");
                }

                var curveServer = new CurveServerFromTextFile(curveFile, calendar);
                AddStatusText($"Há curvas entre {curveServer.MinDate:yyyy-MM-dd} e {curveServer.MaxDate:yyyy-MM-dd} a partir do arquivo {curveFile}.");
                
                _calendar = calendar;
                _curveServer = curveServer;

                comboBoxMode.Items.Clear();
                //comboBoxMode.DataSource = EnumHelper.GetEnumDescriptions<CalculationMode>().ToList();
                comboBoxMode.DataSource = new [] { new EnumDescription<CalculationMode>(CalculationMode.PositionInterpolated)};

                var stressParameters = new StressParameters();
                var defaultStress = stressParameters.StressParallel*100.0;
                textBoxStress.Text = defaultStress.ToString("N1");

                var netWorth = 10e6;
                textBoxNetworth.Text = netWorth.ToString("N0");

            }
            catch (Exception ex)
            {
                buttonExecute.Enabled = false;
                AddStatusText(ex.ToString());
                MessageBox.Show(ex.ToString(), "Erro", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
            finally
            {
                Cursor = Cursors.Default;
            }
        }
    }
}
