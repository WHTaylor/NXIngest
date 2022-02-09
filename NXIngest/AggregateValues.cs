namespace NXIngest
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
    }
}
