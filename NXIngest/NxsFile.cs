using System;
using System.Collections.Generic;
using HDF5.NET;

namespace NXIngest
{
    public class NxsFile
    {
        private readonly H5File _nxs;
        private readonly Dictionary<string, uint[]> _datasetCache = new();

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
            var arr = LoadArray(path);
            Func<uint[], double> f = function switch
            {
                "MAX" => MatrixOperations.Max,
                "MIN" => MatrixOperations.Min,
                "AVG" => MatrixOperations.Avg,
                "SUM" => MatrixOperations.Sum,
                "STD" => MatrixOperations.Std,
                _ => throw new Exception(
                    $"Unknown aggregate function '{function}'"),
            };
            return f(arr).ToString();
        }

        private uint[] LoadArray(string path)
        {
            if (_datasetCache.ContainsKey(path))
            {
                return _datasetCache[path];
            }

            var dataset = _nxs.Dataset(path);
            var arr = dataset.Read<uint>();
            _datasetCache[path] = arr;
            return arr;
        }
    }
}
