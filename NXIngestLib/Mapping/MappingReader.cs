using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Xml;

namespace NXIngest.Mapping
{
    /// <summary>
    /// Converts elements in a mapping file into commands for XmlBuilder.
    ///
    /// Iterating over a MappingReader does a depth first walk over the mapping
    /// file elements, converting each element of a relevant type into a
    /// MappingCommand containing the appropriate values from the element
    /// which can be used to modify the state of an XmlBuilder.
    /// </summary>
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
            return ConvertNodeToCommands(_root).GetEnumerator();
        }

        private static IEnumerable<MappingCommand> ConvertNodeToCommands(
            XmlNode current)
        {
            if (current.Name == "keyword")
            {
                var keywords = ParseKeyword(current);
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
                    foreach (var res in ConvertNodeToCommands(c))
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

        private static MappingCommand ParseKeyword(XmlNode node)
        {
            var nodeValues = GetLeafNodeValues(
                node,
                new List<string> { "value" },
                new List<string> { "value" });
            if (nodeValues == null) return null;
            var (value, valueType) = nodeValues["value"];
            return new AddKeywords(value, valueType);
        }

        /// <summary>
        /// Extract values for a leaf node.
        ///
        /// Other than 'tbl' elements, which can have arbitrarily nested children,
        /// elements in the mapping file become elements of depth of at most 1
        /// in the output. The children of the element in the mapping file become
        /// either the value of the output element, or a leaf child of the output
        /// element.
        ///
        /// This method extracts all of the values from the required and optional
        /// child nodes of a mapping file node, which are then used to populate a
        /// command which will create the appropriate output element.
        /// </summary>
        /// <param name="node">The node to get the values for</param>
        /// <param name="requiredElements">Child elements that must exist for
        /// the node to be valid. If any are missing, the node is ignored.</param>
        /// <param name="requiredTypes">Child elements that must have a type
        /// attribute. If any don't the node is ignored.</param>
        /// <param name="optionalElements">Elements that will be included in the
        /// output if they exist, but can be empty/non-existent.</param>
        /// <returns>A dictionary of elementName: (value, typeAttribute), or null
        /// if any of the requireElements or requiredTypes are not present.</returns>
        private static Dictionary<string, (string, string)> GetLeafNodeValues(
            XmlNode node,
            ICollection<string> requiredElements,
            IEnumerable<string> requiredTypes,
            IEnumerable<string> optionalElements = null)
        {
            (string, string) GetNodeValueAndType(XmlNode n)
            {
                var value = n.InnerText.Trim();
                var type = n.Attributes?["type"]?.Value;
                if (type != "special")
                {
                    return (value, type);
                }

                // Special type nodes have their type as part of their value,
                // separated by a colon ie. 'time:now' or 'sys:location'
                var specialParts = value.Split(":", 2)
                    .Select(p => p.Trim()).ToArray();
                return (specialParts[1], specialParts[0]);
            }

            var childrenToGet = requiredElements.Concat(
                optionalElements ?? Enumerable.Empty<string>());

            var res = node.Children()
                .Where(c => childrenToGet.Contains(c.Name))
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
