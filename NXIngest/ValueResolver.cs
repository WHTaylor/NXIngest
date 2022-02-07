using System;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;

namespace NXIngest
{
    public class ValueResolver
    {
        private readonly FileInfo _nxsFileInfo;
        private readonly NxsFile _nxs;

        public ValueResolver(string nexusFilePath)
        {
            _nxsFileInfo = new FileInfo(nexusFilePath);
            _nxs = new NxsFile(nexusFilePath);
        }

        public string Resolve(string valueLiteral, MappingValueType mappingValueType)
        {
            return mappingValueType switch
            {
                MappingValueType.Fix => valueLiteral,
                MappingValueType.Mix => GetMixedValue(valueLiteral),
                MappingValueType.Nexus => GetNexusValue(valueLiteral),
                MappingValueType.Time => GetTimeValue(valueLiteral),
                MappingValueType.Sys => GetSysValue(valueLiteral),
                _ => throw new ArgumentOutOfRangeException(nameof(mappingValueType),
                    mappingValueType, null)
            };
        }

        private string GetTimeValue(string valueLiteral)
        {
            var parts = valueLiteral.Split(";")
                .Select(p => p.Trim()).ToList();
            var timeLiteral = parts[0];
            var timeFormat = parts.Count > 1 ? parts[1] : "s";
            // TODO: Get time from nexus file for time:nexus values
            var time = DateTime.Now;
            return time.ToString(timeFormat);
        }

        private string GetSysValue(string valueLiteral) =>
            valueLiteral switch
            {
                "size" => _nxsFileInfo.Length.ToString(),
                "location" => _nxsFileInfo.FullName,
                "filename" => _nxsFileInfo.Name,
                _ => throw new Exception($"Unknown sys value '{valueLiteral}")
            };

        private string GetMixedValue(string valueLiteral) =>
            string.Join(
                "",
                valueLiteral.Split("|")
                    .Select(p => p.Trim())
                    .Select(p => p.Split(":"))
                    .Select(p => Resolve(
                        p[1],
                        Enum.Parse<MappingValueType>(p[0].Capitalized()))));

        private const string NexusPathPattern =
            @"(?<path>[^\[]+)(\[(?<aggregateFunction>.+)\])?";
        private string GetNexusValue(string valueLiteral)
        {
            var match = Regex.Match(valueLiteral, NexusPathPattern);
            var path = match.Groups["path"];
            var aggregateFunction = match.Groups["aggregateFunction"];

            return aggregateFunction.Success
                ? _nxs.Aggregate(path.Value, aggregateFunction.Value)
                : _nxs.ReadPath(valueLiteral);
        }
    }
}
