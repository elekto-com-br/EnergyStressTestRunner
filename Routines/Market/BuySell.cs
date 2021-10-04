using System.ComponentModel;

namespace VoltElekto.Market
{
    /// <summary>
    /// Compra ou Venda?
    /// </summary>
    public enum BuySell
    {
        /// <summary>
        /// Compra
        /// </summary>
        [Description("Compra")]
        Buy = 1,

        /// <summary>
        /// Venda
        /// </summary>
        [Description("Venda")]
        Sell = -1,
    }
}