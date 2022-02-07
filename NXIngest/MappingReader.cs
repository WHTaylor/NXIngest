using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Xml;

namespace NXIngest
{
    public class MappingReader : IEnumerable<MappingCommand>
    {
        private readonly XmlNode _root;

        public MappingReader(string path)
        {
            var doc = new XmlDocument();
            doc.Load(path);
            var icat = doc.GetElementsByTagName("icat");
            if (icat.Count == 0)
            {
                throw new Exception("No 'icat' element in mapping file");
            }

            _root = icat[0];
        }

        public IEnumerator<MappingCommand> GetEnumerator()
        {
            return Something(_root).GetEnumerator();
        }

        private IEnumerable<MappingCommand> Something(XmlNode current)
        {
            if (current.Name == "keyword")
            {
                var keywords = ParseAsKeyword(current);
                if (keywords != null) yield return keywords;
            }
            else if (current.Name == "parameter")
            {
                var parameter = ParseParameter(current);
                if (parameter != null) yield return parameter;
            }
            else if (current.Name == "record")
            {
                var record = ParseRecord(current);
                if (record != null) yield return record;
            }
            else if (current.Attributes?["type"]?.Value == "tbl")
            {
                yield return ParseStart(current);
                foreach (var c in current.Children())
                {
                    foreach (var res in Something(c))
                    {
                        yield return res;
                    }
                }

                yield return new EndTable();
            }
            else
            {
                //Console.WriteLine($"Skipping {current.Name}");
            }
        }

        IEnumerator IEnumerable.GetEnumerator()
        {
            return GetEnumerator();
        }

        private static MappingCommand ParseStart(XmlNode node)
        {
            var attrs = node.AttributeItems()
                .Where(e => e.Name != "type")
                .ToDictionary(e => e.Name, e => e.Value);

            return new StartTable(node.Name, attrs);
        }

        private static MappingCommand ParseRecord(XmlNode node)
        {
            var nodeValues = GetLeafNodeValues(
                node,
                new List<string> { "value", "icat_name" },
                new List<string> { "value" });
            if (nodeValues == null) return null;
            var (value, valueType) = nodeValues["value"];
            var (name, _) = nodeValues["icat_name"];

            return new AddRecord(name, value, valueType);
        }

        private static MappingCommand ParseParameter(XmlNode node)
        {
            var nodeValues = GetLeafNodeValues(
                node,
                new List<string> { "value", "icat_name" },
                new List<string> { "value", "units" },
                new List<string> { "units", "description" });
            if (nodeValues == null) return null;

            var (value, valueType) = nodeValues["value"];
            var (name, _) = nodeValues["icat_name"];
            var (units, unitsType) = nodeValues.GetValueOrDefault("units");
            var (description, _) = nodeValues.GetValueOrDefault("description");
            var type = node.Attributes?["type"]?.Value;

            return new AddParameter(
                name, value, valueType, units, unitsType, description, type);
        }

        private static MappingCommand ParseAsKeyword(XmlNode node)
        {
            var nodeValues = GetLeafNodeValues(
                node,
                new List<string> { "value" },
                new List<string> { "value" });
            if (nodeValues == null) return null;
            var (value, valueType) = nodeValues["value"];
            return new AddKeywords(value, valueType);
        }

        private static Dictionary<string, (string, string)> GetLeafNodeValues(
            XmlNode node,
            ICollection<string> requiredElements,
            IEnumerable<string> requiredTypes,
            IEnumerable<string> optionalElements = null)
        {
            (string, string) GetNodeValueAndType(XmlNode n)
            {
                var type = n.Attributes?["type"]?.Value;
                if (type != "special")
                {
                    return (n.InnerText.Trim(), type);
                }

                // Special type nodes have their type as part of their value,
                // separated by a colon ie. 'time:now' or 'sys:location'
                var specialParts = n.InnerText.Trim().Split(":", 2);
                return (specialParts[1], specialParts[0]);
            }

            var elementsToGet = requiredElements.Concat(
                optionalElements ?? Enumerable.Empty<string>());

            var res = node.Children()
                .Where(c => elementsToGet.Contains(c.Name))
                .ToDictionary(
                    c => c.Name,
                    GetNodeValueAndType);

            if (requiredElements.Any(e => !res.ContainsKey(e))
                || requiredTypes.Any(e =>
                    res.ContainsKey(e) &&
                    string.IsNullOrWhiteSpace(res[e].Item2)))
            {
                return null;
            }

            return res;
        }
    }
}
