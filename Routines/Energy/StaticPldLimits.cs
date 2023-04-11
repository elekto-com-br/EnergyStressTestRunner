﻿using System.Collections.Generic;

namespace VoltElekto.Energy
{
    /// <summary>
    /// Limites de PLD
    /// </summary>
    /// <remarks>
    /// Hardcoded
    /// </remarks>
    public class StaticPldLimits : PldLimitsBase
    {
        public StaticPldLimits()
        {
            PldLimits =new Dictionary<int, (double min, double max)>
            {
                { 2016, (30.25, 422.56) },
                { 2017, (33.68, 533.82) },
                { 2018, (40.16, 505.18) },
                { 2019, (42.35, 513.89) },
                { 2020, (39.68, 559.75) },
                { 2021, (49.77, 583.88) },
                { 2022, (55.70, 646.58) },
                { 2023, (69.04, 684.73) }
            };
        }

        

        
    }
}