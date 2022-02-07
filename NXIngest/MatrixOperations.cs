using System;

namespace NXIngest
{
    public static class MatrixOperations
    {
        public static double Max(uint[] arr)
        {
            var max = double.MinValue;
            for (var i = 0; i < arr.Length; i++)
            {
                if (arr[i] > max) max = arr[i];
            }
            return max;
        }

        public static double Min(uint[] arr)
        {
            var min = double.MaxValue;
            for (var i = 0; i < arr.Length; i++)
            {
                if (arr[i] < min) min = arr[i];
            }
            return min;
        }

        public static double Sum(uint[] arr)
        {
            var sum = 0.0;
            for (var i = 0; i < arr.Length; i++)
            {
                sum += arr[i];
            }
            return sum;
        }

        public static double Avg(uint[] arr) => Sum(arr) / arr.Length;

        public static double Std(uint[] arr)
        {
            var mean = Avg(arr);
            var squareSum = 0.0;
            for (var i = 0; i < arr.Length; i++)
            {
                squareSum += Math.Pow(arr[i], 2);
            }

            var squareMean = squareSum / arr.Length;
            return Math.Sqrt(squareMean - Math.Pow(mean, 2));
        }
    }
}
