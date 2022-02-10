using System;

namespace NXIngest.Nexus
{
    public static class MatrixOperations
    {
        public static AggregateValues CalculateAggregates(uint[] arr)
        {
            var min = uint.MaxValue;
            var max = uint.MinValue;
            var total = 0UL;
            var squaredTotal = 0.0;
            for (ulong i = 0; i < (ulong)arr.Length; i++)
            {
                var val = arr[i];
                if (val > max) max = val;
                if (val < min) min = val;
                total += val;
                squaredTotal += Math.Pow(val, 2);
            }

            var mean = (double)total / arr.Length;
            var std = Math.Sqrt(squaredTotal / arr.Length - Math.Pow(mean, 2));
            return new AggregateValues(
                min, max, mean, std, total);
        }

        public static ArrayCounts GetArrayCounts(uint[] arr)
        {
            var min = uint.MaxValue;
            var max = uint.MinValue;
            var total = 0UL;
            var squaredTotal = 0.0;
            for (ulong i = 0; i < (ulong)arr.Length; i++)
            {
                var val = arr[i];
                if (val > max) max = val;
                if (val < min) min = val;
                total += val;
                squaredTotal += Math.Pow(val, 2);
            }

            return new (min, max, total, squaredTotal, (uint)arr.Length);
        }
    }
}
