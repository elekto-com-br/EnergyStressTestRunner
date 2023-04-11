using System.Collections.Generic;
using System.Globalization;
using System.Text;

namespace VoltElekto.Energy;

public class TextFilePldLimits : PldLimitsBase
{
        
    public TextFilePldLimits(string fileName)
    {
        PldLimits = new Dictionary<int, (double min, double max)>();

        var lines = System.IO.File.ReadAllLines(fileName, Encoding.UTF8);
        foreach (var line in lines)
        {
            var parts = line.Split('\t');
            var year = int.Parse(parts[0]);
            var min = double.Parse(parts[1], NumberStyles.Any, CultureInfo.InvariantCulture);
            var max = double.Parse(parts[2], NumberStyles.Any, CultureInfo.InvariantCulture);
            PldLimits.Add(year, (min, max));
        }
    }
}