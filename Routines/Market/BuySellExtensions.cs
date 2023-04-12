using System;

namespace VoltElekto.Market
{
    /// <summary>
    /// Extensões para BuySell
    /// </summary>
    public static class BuySellExtensions
    {
        /// <summary>
        /// Parses the specified obj.
        /// </summary>
        /// <param name="obj">The obj.</param>
        /// <returns></returns>
        public static BuySell Parse(object obj)
        {
            if (obj == null)
            {
                throw new ArgumentNullException(nameof(obj));
            }

            var x = obj.ToString().Trim();
            if (string.IsNullOrWhiteSpace(x))
            {
                return BuySell.Buy;
            }

            if (Enum.IsDefined(typeof(BuySell), obj.ToString()))
            {
                return (BuySell)Enum.Parse(typeof(BuySell), obj.ToString());
            }
            
            if (x.Equals("B", StringComparison.InvariantCultureIgnoreCase)
                || x.Equals("C", StringComparison.InvariantCultureIgnoreCase)
                || x.Equals("Buy", StringComparison.InvariantCultureIgnoreCase)
                || x.Equals("+1", StringComparison.InvariantCultureIgnoreCase)
                || x.Equals("1", StringComparison.InvariantCultureIgnoreCase)
                || x.Equals("+", StringComparison.InvariantCultureIgnoreCase)
                || x.Equals("Compra", StringComparison.InvariantCultureIgnoreCase))
            {
                return BuySell.Buy;
            }
            if (x.Equals("S", StringComparison.InvariantCultureIgnoreCase)
                || x.Equals("V", StringComparison.InvariantCultureIgnoreCase)
                || x.Equals("Sell", StringComparison.InvariantCultureIgnoreCase)
                || x.Equals("-1", StringComparison.InvariantCultureIgnoreCase)
                || x.Equals("-", StringComparison.InvariantCultureIgnoreCase)
                || x.Equals("Venda", StringComparison.InvariantCultureIgnoreCase))
            {
                return BuySell.Sell;
            }

            throw new FormatException($"Valor {obj} não é um enumerável BuySell válido");
        }

        public static double GetSignal(this BuySell buySell)
        {
            return buySell == BuySell.Buy ? 1.0 : -1.0;
        }

    }
}