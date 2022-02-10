using System;
using System.Collections.Generic;
using HDF5.NET;
using log4net;

namespace NXIngest.Nexus
{
    /// <summary>
    /// Provides convenient methods for extracting values from a nexus file
    /// </summary>
    public class NxsFile
    {
        private readonly ILog _log = LogManager.GetLogger(typeof(NxsFile));
        private readonly H5File _nxs;

        private readonly Dictionary<string, AggregateValues> _aggregateCache =
            new();

        public NxsFile(string path)
        {
            _nxs = H5File.OpenRead(path);
        }

        public string ReadPath(string path)
        {
            if (path.Contains("."))
            {
                var parts = path.Split(".");
                return ReadAttribute(parts[0], parts[1]);
            }

            var ds = _nxs.Dataset(path);
            if (ds.Space.Rank != 1)
            {
                _log.Warn(
                    $"Expected a single value at '{path}', got array of rank " +
                    $"{ds.Space.Rank}. Ignoring element.");
                return null;
            }

            var res = ds.Type.Class switch
            {
                H5DataTypeClass.String => ds.ReadString()[0],
                H5DataTypeClass.VariableLength => ds.ReadString()[0],
                H5DataTypeClass.FloatingPoint => ds.Read<float>()[0].ToString(),
                H5DataTypeClass.FixedPoint => ds.Read<int>()[0].ToString(),
                _ => throw new Exception(
                    $"Cannot read dataset value of type {ds.Type.Class}. " +
                    "Must be a string, integer, or floating point value")
            };
            return res.Trim();
        }

        private string ReadAttribute(string path, string attribute)
        {
            var obj = _nxs.Get(path) as H5AttributableObject;
            var attr = obj?.Attribute(attribute);
            var res = attr?.Type.Class switch
            {
                H5DataTypeClass.String => attr.ReadString()[0],
                H5DataTypeClass.VariableLength => attr.ReadString()[0],
                H5DataTypeClass.FloatingPoint => attr.Read<float>()[0].ToString(),
                H5DataTypeClass.FixedPoint => attr.Read<int>()[0].ToString(),
                null => null,
                _ => throw new Exception(
                    $"Cannot read dataset attribute of type {attr.Type.Class}. " +
                    "Must be a string, integer, or floating point value")
            };
            return res?.Trim();
        }

        public string Aggregate(string path, string function)
        {
            var aggregates = GetAggregateValues(path);
            if (aggregates == null) return null;
            return function switch
            {
                "MAX" => aggregates.Max.ToString(),
                "MIN" => aggregates.Min.ToString(),
                "AVG" => aggregates.Avg.ToString(),
                "SUM" => aggregates.Sum.ToString(),
                "STD" => aggregates.Std.ToString(),
                _ => throw new Exception(
                    $"Unknown aggregate function '{function}'. Must be one of" +
                    "'MAX', 'MIN', 'AVG', 'SUM' or 'STD'."),
            };
        }

        private AggregateValues GetAggregateValues(string path)
        {
            if (_aggregateCache.ContainsKey(path))
            {
                return _aggregateCache[path];
            }

            var dataset = _nxs.Dataset(path);
            if (dataset.Space.Rank is < 2 or > 3)
            {
                _log.Warn(
                    "Can only calculate aggregates for 2d or 3d arrays, " +
                    $"'{path}' is a {dataset.Space.Rank}d array");
                return null;
            }

            AggregateValues res;
            if (dataset.Space.Rank == 2)
            {
                var arr = dataset.Read<uint>();
                res = MatrixOperations.CalculateAggregates(arr);
            }
            else
            {
                /*
                 * Some of the datasets we have to aggregate don't fit in memory.
                 * To work around this, we break the dataset up into 'slabs' that
                 * will fit. Each slab is (x, y, z) in size, which is determined
                 * by the algorithm described by CalculateStepSize.
                 *
                 * Note: (x, y, z) is equal to dims if the full dataset will fit
                 * in the quarter GB memory we've allotted as the maximum, which
                 * is the case for the majority of datasets. In these cases,
                 * everything is read in a single iteration.
                 */
                var dims = dataset.Space.Dimensions;
                var counts = new List<ArrayCounts>();
                var (x, y, z) = CalculateStepSize((dims[0], dims[1], dims[2]));
                var outputSize = x * y * z;

                // For each step of (x, y, z), aggregate the counts of the dataset
                for (var i = 0UL; i < dims[0]; i += x)
                {
                    for (var j = 0UL; j < dims[1]; j += y)
                    {
                        for (var k = 0UL; k < dims[2]; k += z)
                        {
                            var fileSelection = new HyperslabSelection(
                                3,
                                new[] { i, j, k },
                                new[] { x, y, z });
                            var memorySelection =
                                new HyperslabSelection(0, outputSize);
                            var arr = dataset.Read<uint>(fileSelection,
                                memorySelection);
                            var thisCount = MatrixOperations.GetArrayCounts(arr);
                            counts.Add(thisCount);
                        }
                    }
                }

                // Combine the step counts into the final aggregate values
                res = AggregateValues.FromCounts(counts);
            }

            _aggregateCache[path] = res;
            return res;
        }

        // Number of uints in 1/4 of a GB
        private const ulong MaxSlabSize = 1073741824 / (4 * 4);

        /// <summary>
        /// Calculates the best step size for iterating through a dataset
        ///
        /// The step size must be a factor of the total size in all
        /// dimensions, and a step must fit into the allotted memory. Each step
        /// of size (x, y, z) take up x * y * z * sizeof(uint) bytes.
        ///
        /// The data is stored in memory in 'C order', meaning it's laid out
        /// by the z axis first, then y axis, then x axis. This means we want to
        /// maximize z first, then y, then x, to read as much contiguous data
        /// as possible per slab.
        /// </summary>
        /// <param name="dims">The dimensions of the dataset</param>
        /// <returns>The largest step that will fit into memory that's a factor
        /// of dims</returns>
        private static (ulong, ulong, ulong) CalculateStepSize(
            (ulong, ulong, ulong) dims)
        {
            var (xSize, ySize, zSize) = dims;
            if (zSize > MaxSlabSize)
            {
                throw new Exception(
                    "Can't even fit one z slice into memory, giving up");
            }

            while (xSize * ySize * zSize > MaxSlabSize)
            {
                xSize /= SmallestFactor(xSize);

                // Keep trying to make x smaller if needed before moving onto y
                if (xSize != 1) continue;

                while (ySize * zSize > MaxSlabSize)
                {
                    ySize /= SmallestFactor(ySize);
                }
            }

            return (xSize, ySize, zSize);
        }

        private static ulong SmallestFactor(ulong n)
        {
            for (var i = 2UL; i < n; i++)
            {
                if (n % i == 0) return i;
            }

            return n;
        }
    }
}
