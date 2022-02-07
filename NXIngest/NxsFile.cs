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
            try
            {
                // TODO: Don't assume this is a single string
                return _nxs.Dataset(path).ReadString()[0];
            }
            catch
            {
                return $"{path} NOT FOUND";
            }
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
