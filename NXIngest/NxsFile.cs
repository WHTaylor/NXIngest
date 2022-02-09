using System;
using System.Collections.Generic;
using System.Linq;
using HDF5.NET;
using log4net;

namespace NXIngest
{
    public class NxsFile
    {
        private readonly ILog _log = LogManager.GetLogger(typeof(NxsFile));
        private readonly H5File _nxs;
        private readonly Dictionary<string, AggregateValues> _aggregateCache = new();

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
                _ => throw new Exception(ds.Type.Class.ToString())
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
                _ => throw new Exception(attr.Type.Class.ToString())
            };
            return res?.Trim();
        }

        public string Aggregate(string path, string function)
        {
            var aggregates = GetAggregateValues(path);
            return function switch
            {
                "MAX" => aggregates.Max.ToString(),
                "MIN" => aggregates.Min.ToString(),
                "AVG" => aggregates.Avg.ToString(),
                "SUM" => aggregates.Sum.ToString(),
                "STD" => aggregates.Std.ToString(),
                _ => throw new Exception(
                    $"Unknown aggregate function '{function}'"),
            };
        }

        private AggregateValues GetAggregateValues(string path)
        {
            if (_aggregateCache.ContainsKey(path))
            {
                return _aggregateCache[path];
            }
            var dataset = _nxs.Dataset(path);
            var dims = dataset.Space.Dimensions;

            const int uintsInAGb = 1_000_000_000 / 32;
            if (dims.Length > 3)
            {
                throw new Exception("Can only aggregate 2d or 3d arrays, " +
                    $"'{path}' is a {dims.Length}d array");
            }

            AggregateValues res;
            if (dims.Length < 3 || dims.Aggregate(1UL, (a, b) => a * b) < uintsInAGb)
            {
                var arr = dataset.Read<uint>();
                res = MatrixOperations.CalculateAggregates(arr);
            }
            else
            {
                var counts = new List<ArrayCounts>();
                var numArrays = dims[^1];
                const int atATime = 149;
                var toGet = new[] { 0UL, 0UL, 0UL };
                var arraySize = new[] { dims[0], dims[1], (ulong)atATime };
                var outputSize = dims[0] * dims[1] * atATime;
                for (var i = 0UL; i < numArrays; i++)
                {
                    toGet[2] = i * atATime;
                    var fileSelection = new HyperslabSelection(
                        3,
                        toGet,
                        arraySize);
                    var memorySelection = new HyperslabSelection(0, outputSize);
                    var arr = dataset.Read<uint>(fileSelection, memorySelection);
                    var thisCount = MatrixOperations.GetArrayCounts(arr);
                    Console.WriteLine($"{i}: {thisCount.Sum}");
                    counts.Add(thisCount);
                }

                res = AggregateValues.FromCounts(counts);
            }

            _aggregateCache[path] = res;
            return res;
        }
    }
}
