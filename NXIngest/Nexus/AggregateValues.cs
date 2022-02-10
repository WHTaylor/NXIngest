using System;
using System.Collections.Generic;
using System.Linq;

namespace NXIngest.Nexus
{
    public class AggregateValues
    {
        public readonly uint Min;
        public readonly uint Max;
        public readonly ulong Sum;
        public readonly double Avg;
        public readonly double Std;

        public AggregateValues(
            uint min, uint max, double avg, double std, ulong sum)
        {
            Min = min;
            Max = max;
            Avg = avg;
            Std = std;
            Sum = sum;
        }

        public static AggregateValues FromCounts(IList<ArrayCounts> counts)
        {
            var min = counts.Select(c => c.Min).Min();
            var max = counts.Select(c => c.Max).Max();
            var sum = counts.Aggregate(0UL, (i, c) => i + c.Sum);
            var count = counts.Aggregate(0, (i, c) => i + c.Count);
            var mean = (double)sum / count;
            var squaresSum = counts.Aggregate(0.0, (i, c) => i + c.SquaredTotal);
            var squaresMean = squaresSum / count;
            var std = Math.Sqrt(squaresMean - Math.Pow(mean, 2));
            return new(min, max, mean, std, sum);
        }
    }
}
