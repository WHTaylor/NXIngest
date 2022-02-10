namespace NXIngest.Nexus
{
    public class ArrayCounts
    {
        public readonly uint Min;
        public readonly uint Max;
        public readonly ulong Sum;
        public readonly double SquaredTotal;
        public readonly int Count;

        public ArrayCounts(uint min, uint max, ulong sum, double squaredTotal, int count)
        {
            Min = min;
            Max = max;
            Sum = sum;
            SquaredTotal = squaredTotal;
            Count = count;
        }
    }
}
