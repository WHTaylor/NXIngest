using System;
using System.Collections.Generic;
using HDF5.NET;

namespace NXIngest
{
    public class NxsFile
    {
        private readonly H5File _nxs;

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
    }
}
