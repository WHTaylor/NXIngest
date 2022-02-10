using System;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using NXIngest.Nexus;

namespace NXIngest.Output
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
            var trimmed = valueLiteral.Trim();
            return mappingValueType switch
            {
                MappingValueType.Fix => trimmed,
                MappingValueType.Mix => GetMixedValue(trimmed),
                MappingValueType.Nexus => GetNexusValue(trimmed),
                MappingValueType.Time => GetTimeValue(trimmed),
                MappingValueType.Sys => GetSysValue(trimmed),
                _ => throw new ArgumentOutOfRangeException(nameof(mappingValueType),
                    mappingValueType, null)
            };
        }

        private const string NexusTimePattern = @"nexus\((?<path>[^)]+)\)";
        private string GetTimeValue(string valueLiteral)
        {
            var parts = valueLiteral.Split(";")
                .Select(p => p.Trim()).ToList();
            var timeLiteral = parts[0];
            DateTime time;

            if (timeLiteral.ToLower() == "now")
            {
                time = DateTime.Now;
            }
            else
            {
                var match = Regex.Match(timeLiteral, NexusTimePattern);
                if (!match.Success)
                {
                    throw new Exception(
                        $"{timeLiteral} must be either 'now' or 'nexus(/path/to/value)'");
                }

                var path = match.Groups["path"].Value;
                time = DateTime.Parse(GetNexusValue(path));
            }

            var timeFormat = parts.Count > 1 ? parts[1] : "s";
            return DateTimeFormatter.Format(time, timeFormat);
        }

        private string GetSysValue(string valueLiteral) =>
            valueLiteral switch
            {
                "size" => _nxsFileInfo.Length.ToString(),
                "location" => _nxsFileInfo.FullName,
                "filename" => _nxsFileInfo.Name,
                _ => throw new Exception(
                    $"Unknown sys value '{valueLiteral}', must be one of " +
                    "'size', 'location', or 'filename'")
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
