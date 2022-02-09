using System;
using System.Collections.Generic;
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
            var arr = dataset.Read<uint>();
            var res = MatrixOperations.CalculateAggregates(arr);
            _aggregateCache[path] = res;
            return res;
        }
    }
}
