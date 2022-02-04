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
            string name = null;
            string value = null;
            string valueType = null;
            foreach (var c in node.Children())
            {
                switch (c.Name)
                {
                    case "icat_name":
                        name = c.InnerText.Trim();
                        break;
                    case "value":
                        valueType = c.Attributes?["type"]?.Value;
                        if (valueType == "special")
                        {
                            var parts = c.InnerText.Trim().Split(":");
                            valueType = parts[0];
                            value = parts[1];
                        }
                        else
                        {
                            value = c.InnerText.Trim();
                        }
                        break;
                    // TODO: default to log warning for unrecognised element
                }
            }

            if (name == null || value == null || valueType == null) return null;
            return new AddRecord(name, value, valueType);
        }

        private static MappingCommand ParseParameter(XmlNode node)
        {
            string name = null;
            string value = null;
            string valueType = null;
            string units = null;
            string unitsType = null;
            string description = null;
            foreach (var c in node.Children())
            {
                switch (c.Name)
                {
                    case "icat_name":
                        name = c.InnerText.Trim();
                        break;
                    case "value":
                        valueType = c.Attributes?["type"]?.Value;
                        if (valueType == "special")
                        {
                            var parts = c.InnerText.Trim().Split(":");
                            valueType = parts[0];
                            value = parts[1];
                        }
                        else
                        {
                            value = c.InnerText.Trim();
                        }
                        break;
                    case "units":
                        unitsType = c.Attributes?["type"]?.Value;
                        if (unitsType == "special")
                        {
                            var parts = c.InnerText.Trim().Split(":");
                            unitsType = parts[0];
                            units = parts[1];
                        }
                        else
                        {
                            units = c.InnerText.Trim();
                        }
                        break;
                    case "description":
                        description = c.InnerText.Trim();
                        break;
                    // TODO: default to log warning for unrecognised element
                }
            }

            var type = node.Attributes?["type"]?.Value;

            if (name == null || value == null || valueType == null || description == null || type == null) return null;
            return new AddParameter(name, value, valueType, units, unitsType, description, type);
        }

        private static MappingCommand ParseAsKeyword(XmlNode node)
        {
            string value = null;
            string valueType = null;
            foreach (var c in node.Children())
            {
                switch (c.Name)
                {
                    case "value":
                        valueType = c.Attributes?["type"]?.Value;
                        if (valueType == "special")
                        {
                            var parts = c.InnerText.Trim().Split(":");
                            valueType = parts[0];
                            value = parts[1];
                        }
                        else
                        {
                            value = c.InnerText.Trim();
                        }
                        break;
                    // TODO: default to log warning for unrecognised element
                }
            }

            if (value == null || valueType == null) return null;
            return new AddKeywords(value, valueType);
        }
    }
}
