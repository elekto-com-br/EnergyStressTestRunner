using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using OfficeOpenXml;
using VoltElekto.Calendars;
using VoltElekto.Data;
using VoltElekto.Market;

namespace VoltElekto.Energy
{
    /// <summary>
    /// Classe principal de cálculo
    /// </summary>
    public class Calculator
    {
        private readonly ICalendar _calendar;
        private readonly ICurveServer _curveServer;
        private readonly PldLimits _pldLimits = new();

        public Calculator(ICalendar calendar, ICurveServer curveServer)
        {
            _calendar = calendar;
            _curveServer = curveServer;
        }

        /// <summary>
        /// Devolve as posições em cada dia útil, interpolando entre as datas chave
        /// </summary>
        public IEnumerable<EnergyPosition> GetInterpolatedAllPositions(List<EnergyPosition> rawPositions, out List<EnergyPosition> allTrades)
        {
            foreach (var position in rawPositions)
            {
                position.CompleteValues(_calendar);
            }

            var referencePositions = rawPositions.OrderBy(p => p.ReferenceDate).ThenBy(p => p.StartMonth).ThenBy(p => p.BuySell).ToArray();

            // Consolida as posições para net existentes
            referencePositions = Consolidate(referencePositions).ToArray();

            // computa as datas chaves (posição arbitradas)
            var firstDate = _calendar.GetPrevWorkday(referencePositions.Min(p => p.ReferenceDate));
            var keyDates = (new[] { firstDate }).Concat(referencePositions.Select(p => p.ReferenceDate).Distinct()).OrderBy(d => d).ToArray();

            // Dicionario para as posições de referência em cada data chave
            var keyPositionsByDate = referencePositions.GroupBy(p => p.ReferenceDate).ToDictionary(p => p.Key, p => p.Select(p => p).ToArray());

            var allDates = _calendar.GetWorkDates(keyDates[0], keyDates[keyDates.Length - 1], DeltaTerminalDayAdjust.Full).ToArray();

            // Para cada data necessária, vejo se uso direto, ou interpolo posições
            var allPositions = new List<EnergyPosition>();
            for (var i = 1; i < allDates.Length; ++i)
            {
                var date = allDates[i];

                if (keyPositionsByDate.TryGetValue(date, out var positions))
                {
                    // Data chave, nenhuma interpolação necessária
                    allPositions.AddRange(positions);
                    continue;
                }

                // Acho as posições chave anteriores
                var previousDate = keyPositionsByDate.Keys.Where(k => k < date).OrderByDescending(k => k).First();
                var previousPositions = keyPositionsByDate[previousDate].ToDictionary(p => p.StartMonth);

                // Acho as posições chaves posteriores
                var nextDate = keyPositionsByDate.Keys.Where(k => k > date).OrderBy(k => k).First();
                var nextPositions = keyPositionsByDate[nextDate].ToDictionary(p => p.StartMonth);

                var totalPeriod = _calendar.GetDeltaWorkDays(previousDate, nextDate);
                var partialPeriod = _calendar.GetDeltaWorkDays(previousDate, date);
                var partialFraction = partialPeriod / (double)totalPeriod;

                // Distintos produtos considerados
                var productDates = previousPositions.Keys.Concat(nextPositions.Keys).Distinct().OrderBy(d => d).ToArray();

                foreach (var productDate in productDates)
                {
                    var previousAmount = 0.0;
                    if (previousPositions.TryGetValue(productDate, out var p))
                    {
                        previousAmount = p.Amount;
                    }

                    var nextAmount = 0.0;
                    if (nextPositions.TryGetValue(productDate, out p))
                    {
                        nextAmount = p.Amount;
                    }

                    var onDateAmount = (nextAmount - previousAmount) * partialFraction + previousAmount;
                    var pe = new EnergyPosition
                    {
                        ReferenceDate = date,
                        StartMonth = productDate,
                        Amount = onDateAmount
                    };
                    pe.PayDate = _calendar.AddWorkDays(pe.StartMonth.AddMonths(1).AddDays(-1), 6);

                    allPositions.Add(pe);
                }

            }

            // Monta os Trades fazendo o Delta dia-a-dia para cada produto
            var allPositionsByReferenceDate = allPositions.GroupBy(p => p.ReferenceDate).ToDictionary(p => p.Key, p => p.ToDictionary(p => p.StartMonth));

            allTrades = new List<EnergyPosition>();
            for (var i = 1; i < allDates.Length; ++i)
            {
                var previousDate = allDates[i - 1];
                allPositionsByReferenceDate.TryGetValue(previousDate, out var previousPositions);
                previousPositions ??= new Dictionary<System.DateTime, EnergyPosition>();

                var date = allDates[i];
                allPositionsByReferenceDate.TryGetValue(date, out var currentPositions);
                currentPositions ??= new Dictionary<System.DateTime, EnergyPosition>();

                var allProducts = previousPositions.Keys.Concat(currentPositions.Keys).Distinct().OrderBy(dt => dt).ToArray();

                foreach (var product in allProducts)
                {
                    previousPositions.TryGetValue(product, out var previous);
                    previous ??= new EnergyPosition();

                    currentPositions.TryGetValue(product, out var current);
                    current ??= new EnergyPosition();

                    if (Math.Abs(current.Amount - previous.Amount) < FinancialConstants.ValuesEpsilon)
                    {
                        // sem necessidade de fazer qualquer trade
                        continue;
                    }

                    var trade = new EnergyPosition
                    {
                        ReferenceDate = date,
                        StartMonth = product,
                        Amount = current.Amount - previous.Amount,
                    };
                    trade.PayDate = _calendar.AddWorkDays(trade.StartMonth.AddMonths(1).AddDays(-1), 6);

                    allTrades.Add(trade);
                }
            }

            // Completo informação dos trades com o preço fixado, vindo do mercado, normalizo compra e venda etc
            var tradeNumber = 1;
            foreach (var trade in allTrades)
            {
                // Seta a data de pagamento (quando o trade expira)
                trade.PayDate = _calendar.AddWorkDays(trade.StartMonth.AddMonths(1).AddDays(-1), 6);
                trade.TradeDate = trade.ReferenceDate;

                // buscar na projeção de preços
                var curveOnDate = _curveServer.GetCurve(trade.TradeDate);
                trade.TradePrice = curveOnDate.GetValue(trade.PayDate);

                // normaliza compra e venda
                if (trade.Amount < 0.0)
                {
                    trade.Amount = -trade.Amount;
                    trade.BuySell = BuySell.Sell;
                }
                else if (trade.Amount > 0.0)
                {
                    trade.BuySell = BuySell.Buy;
                }
                else
                {
                    // deveria acontecer?
                    trade.BuySell = BuySell.Buy;
                }

                trade.Tag = $"t{tradeNumber}.{trade.TradeDate:yyyy-MM-dd}";
                ++tradeNumber;
            }

            // Computo as posições a serem colocadas em cada data
            var allFinalPositions = new List<EnergyPosition>();
            for (var i = 1; i < allDates.Length; ++i)
            {
                var date = allDates[i];

                // todos os trades que aconteceram antes...
                var validTradesOnDate = allTrades.Where(t => t.ReferenceDate <= date).ToArray();

                // E que ainda não venceram
                validTradesOnDate = validTradesOnDate.Where(t => t.PayDate >= date).ToArray();

                foreach (var trade in validTradesOnDate)
                {
                    var p = new EnergyPosition
                    {
                        ReferenceDate = date,
                        Amount = trade.Amount,
                        BuySell = trade.BuySell,
                        PayDate = trade.PayDate,
                        StartMonth = trade.StartMonth,
                        TradeDate = trade.ReferenceDate,
                        TradePrice = trade.TradePrice,
                        Tag = trade.Tag
                    };
                    allFinalPositions.Add(p);
                }
            }

            return allFinalPositions;
        }

        public List<PortfolioResult> CalculateStress(IEnumerable<EnergyPosition> allFinalPositions, StressParameters stressParameters)
        {
            var portfolios = allFinalPositions.GroupBy(ep => ep.ReferenceDate).OrderBy(p => p.Key);

            var results = new List<PortfolioResult>();

            foreach (var portfolio in portfolios)
            {
                var referenceDate = portfolio.Key;
                var positions = portfolio.ToList();
                var priceOnDate = _curveServer.GetCurve(referenceDate);

                results.Add(CalculatePortfolioStress(referenceDate, positions, priceOnDate, stressParameters));
            }

            return results;
        }

        private PortfolioResult CalculatePortfolioStress(DateTime referenceDate, List<EnergyPosition> positions, ICurve priceOnDate, StressParameters stressParameters)
        {
            
            var pr = new PortfolioResult
            {
                ReferenceDate = referenceDate,
            };

            foreach (var p in positions)
            {
                var payDate = p.PayDate;
                var price = priceOnDate.GetValue(payDate, stressParameters, _pldLimits);

                var positionResult = new EnergyPositionResult(p);
                positionResult.CalculateResults(price);

                pr.Add(positionResult);
            }

            pr.CalculateReferenceStress();

            return pr;
        }


        /// <summary>
        /// Devolve as posições em cada dia útil, sem interpolar. As posições expiram, ou são invertidas/ajustadas nos dias chave
        /// </summary>
        public IEnumerable<EnergyPosition> GetAllPositions(List<EnergyPosition> positions)
        {
            throw new NotImplementedException();
        }

        /// <summary>
        /// Devolve as posições em cada dia útil a partir dos trades feitos
        /// </summary>
        public IEnumerable<EnergyPosition> GetAllPositionsFromTrades(List<EnergyPosition> trades)
        {
            throw new NotImplementedException();
        }

        // Consolida as posições do produto, deixando apenas os nets
        private static IEnumerable<EnergyPosition> Consolidate(IEnumerable<EnergyPosition> referencePositions)
        {
            var byReferenceDate = referencePositions.GroupBy(p => p.ReferenceDate);
            foreach (var positionsOnDate in byReferenceDate)
            {
                var referenceDate = positionsOnDate.Key;
                var byProduct = positionsOnDate.GroupBy(od => od.StartMonth);
                foreach (var position in byProduct)
                {
                    var netAmount = position.Sum(p => p.Amount);
                    if (netAmount == 0.0)
                    {
                        // Net, pode ser ignorado
                        continue;
                    }

                    if (netAmount < 0.0)
                    {
                        // net vendido
                        yield return new EnergyPosition
                        {
                            ReferenceDate = referenceDate,
                            Amount = netAmount,
                            BuySell = BuySell.Sell,
                            StartMonth = position.Key,
                            PayDate = position.First().PayDate
                        };
                    }
                    else
                    {
                        // net comprado
                        yield return new EnergyPosition
                        {
                            ReferenceDate = referenceDate,
                            Amount = netAmount,
                            BuySell = BuySell.Buy,
                            StartMonth = position.Key,
                            PayDate = position.First().PayDate
                        };
                    }

                }

            }

        }

        public void WriteReport(string templateFile, string reportFile, List<PortfolioResult> stresses, List<EnergyPosition> allPositions, List<EnergyPosition> allTrades, double capital, StressParameters stressParameters)
        {
            var file = new FileInfo(templateFile);
            using var package = new ExcelPackage(file);
            var workBook = package.Workbook;

            const double mm = 1_000_000.0;
            
            #region Fator de Alavancagem (o resumo data a data)

            var sheet = workBook.Worksheets["FatorAlavancagem"];

            var startRow = 7;
            var row = startRow;
            foreach (var r in stresses)
            {
                sheet.SetValue(row, 2, r.ReferenceDate);
                sheet.SetValue(row, 3, r.Value/mm);
                sheet.SetValue(row, 4, r.ReferenceStress/mm);
                sheet.SetValue(row, 6, capital/mm);

                ++row;
            }

            #endregion

            #region Detalhes dia-a-dia

            sheet = workBook.Worksheets["ValorDiaADia"];

            startRow = 7;
            row = startRow;
            foreach (var r in stresses)
            {
                sheet.SetValue(row, 2, r.ReferenceDate);
                sheet.SetValue(row, 3, r.Value/mm);
                sheet.SetValue(row, 4, r.FixedValue/mm);
                sheet.SetValue(row, 5, r.EnergyValue/mm);
                sheet.SetValue(row, 6, r.StressParallelPlus/mm);
                sheet.SetValue(row, 7, r.StressParallelMinus/mm);
                sheet.SetValue(row, 8, r.StressShortPlus/mm);
                sheet.SetValue(row, 9, r.StressShortMinus/mm);
                sheet.SetValue(row, 10, r.StressAscendent/mm);
                sheet.SetValue(row, 11, r.StressDescendent/mm);
                sheet.SetValue(row, 12, r.WorstStress);
                sheet.SetValue(row, 13, r.ReferenceStress/mm);

                ++row;
            }

            #endregion

            #region Posições dia-a-dia

            sheet = workBook.Worksheets["PosiçõesDiaADia"];

            startRow = 7;
            row = startRow;
            foreach (var r in stresses)
            {
                foreach (var e in r.Results)
                {
                    sheet.SetValue(row, 2, e.ReferenceDate);
                    sheet.SetValue(row, 3, e.Position.StartMonth);
                    sheet.SetValue(row, 4, e.Position.Tag);
                    sheet.SetValue(row, 5, e.Position.TradeDate);
                    sheet.SetValue(row, 6, e.Position.Amount);
                    sheet.SetValue(row, 7, e.Position.BuySell == BuySell.Buy ? "c" : "v");
                    sheet.SetValue(row, 8, e.Position.TradePrice);
                
                    sheet.SetValue(row, 9, e.Value/mm);
                    sheet.SetValue(row, 10, e.FixedValue/mm);
                    sheet.SetValue(row, 11, e.EnergyValue/mm);

                    sheet.SetValue(row, 12, e.StressParallelPlus/mm);
                    sheet.SetValue(row, 13, e.StressParallelMinus/mm);
                    sheet.SetValue(row, 14, e.StressShortPlus/mm);
                    sheet.SetValue(row, 15, e.StressShortMinus/mm);
                    sheet.SetValue(row, 16, e.StressAscendent/mm);
                    sheet.SetValue(row, 17, e.StressDescendent/mm);
                    sheet.SetValue(row, 18, e.WorstStress);
                    
                    ++row;    
                }
            }

            #endregion

            #region Curvas

            sheet = workBook.Worksheets["Curvas"];

            startRow = 7;
            row = startRow;
            foreach (var r in stresses)
            {
                var referenceDate = r.ReferenceDate;
                var maxDate = r.Results.Select(e => e.Position.PayDate).Max();

                var curve = _curveServer.GetCurve(referenceDate);

                for (var d = referenceDate; d <= maxDate; d = d.AddDays(1))
                {
                    var prices = curve.GetValue(d, stressParameters, _pldLimits);

                    sheet.SetValue(row, 2, referenceDate);
                    sheet.SetValue(row, 3, d);
                    sheet.SetValue(row, 4, _calendar.GetDeltaWorkDays(referenceDate, d));
                    sheet.SetValue(row, 5, _calendar.GetDeltaActualDays(referenceDate, d));
                    sheet.SetValue(row, 6, curve.IsExtrapolated ? "I" : "O");
                    sheet.SetValue(row, 7, prices.zero);
                    sheet.SetValue(row, 8, prices.parallelPlus);
                    sheet.SetValue(row, 9, prices.parallelMinus);
                    sheet.SetValue(row, 10, prices.shortPlus);
                    sheet.SetValue(row, 11, prices.shortMinus);
                    sheet.SetValue(row, 12, prices.ascendent);
                    sheet.SetValue(row, 13, prices.descendent);

                    ++row;
                }

            }

            #endregion

            package.SaveAs(new FileInfo(reportFile));
        }
    }
}
