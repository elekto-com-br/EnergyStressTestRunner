using System.Collections.Generic;

namespace VoltElekto.Energy
{
    public interface IPositionsServer
    {
        IEnumerable<EnergyPosition> GetPositions();
        IEnumerable<EnergyPosition> GetTrades();
    }
}