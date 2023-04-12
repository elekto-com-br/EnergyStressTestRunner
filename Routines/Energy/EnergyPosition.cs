using System;
using System.Globalization;
using VoltElekto.Calendars;
using VoltElekto.Market;

namespace VoltElekto.Energy
{
    /// <summary>
    /// Posição em Energia
    /// </summary>
    public class EnergyPosition
    {
        /// <summary>
        /// Data da Posição
        /// </summary>
        public DateTime ReferenceDate { get; set; }

        /// <summary>
        /// Mês de entrega
        /// </summary>
        /// <remarks>
        /// mês de entrega, 1º dia
        /// </remarks>
        public DateTime StartMonth { get; set; }

        /// <summary>
        /// Data de Pagamento
        /// </summary>
        /// <remarks>
        /// dia de pagamento, 6º útil do mês seguinte a entrega
        /// </remarks>
        public DateTime PayDate { get; set; }

        /// <summary>
        /// Compra ou Venda
        /// </summary>
        public BuySell BuySell { get; set; }

        /// <summary>
        /// Quantidade, Mwm
        /// </summary>
        public double Amount { get; set; }

        /// <summary>
        /// Dia da Negociação
        /// </summary>
        public DateTime TradeDate { get; set; }

        /// <summary>
        /// Valor da perna fixa do trade
        /// </summary>
        public double TradePrice { get; set; }

        /// <summary>
        /// tag para identificar o trade/posição
        /// </summary>
        public string Tag { get; set; }

        /// <summary>
        /// Alias da entrega
        /// </summary>
        public string DeliveryAlias
        {
            get
            {
                var currentMonth = ReferenceDate.GetSerialMonth();
                var startMonth = StartMonth.GetSerialMonth();
                var diff = startMonth - currentMonth;
                return $"M{diff}";
            }
        }

        // O volume, em MWh, com sinal
        public double GetVolume()
        {
            var hours = StartMonth.HoursInMonth();
            var volume = Amount * BuySell.GetSignal() * hours;
            return volume;
        }

        public void CompleteValues(ICalendar calendar)
        {
            ReferenceDate = calendar.GetNextOrSameWorkday(ReferenceDate);
            PayDate = calendar.AddWorkDays(StartMonth.AddMonths(1).AddDays(-1), 6);
            Amount = BuySell.GetSignal() * Amount;
        }

        public override string ToString()
        {
            return $"{BuySell} {Amount:N1} for {StartMonth:yyyy-MM} at {ReferenceDate:yyyy-MM-dd}";
        }
    }
}