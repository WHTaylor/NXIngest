using System.Collections.Generic;
using System.Xml;

namespace NXIngest
{
    public class XmlBuilder
    {
        private readonly XmlDocument _doc = new();
        private readonly Stack<XmlElement> _tableStack = new();
        private readonly ValueResolver _valueResolver;

        public XmlBuilder(ValueResolver valueResolver)
        {
            _valueResolver = valueResolver;
        }

        public void Save(string path)
        {
            var settings = new XmlWriterSettings
            {
                Indent = true
            };
            using var xw = XmlWriter.Create(path, settings);
            _doc.WriteTo(xw);
        }

        public void Execute(MappingCommand cmd)
        {
            switch (cmd)
            {
                case StartTable castCmd: Execute(castCmd); break;
                case EndTable castCmd: Execute(castCmd); break;
                case AddRecord castCmd: Execute(castCmd); break;
                case AddParameter castCmd: Execute(castCmd); break;
                case AddKeywords castCmd: Execute(castCmd); break;
            }
        }

        private void Execute(StartTable cmd)
        {
            var table = _doc.CreateElement(cmd.TableName);
            foreach (var (k, v) in cmd.Attributes)
            {
                table.SetAttribute(k, v);
            }
            CurrentParent.AppendChild(table);
            _tableStack.Push(table);
        }

        private void Execute(EndTable cmd)  => _tableStack.Pop();

        private void Execute(AddRecord cmd)
        {
            var record = _doc.CreateElement(cmd.Name);
            record.InnerText = _valueResolver.Resolve(cmd.Value, cmd.ValueType);
            CurrentParent.AppendChild(record);
        }

        private void Execute(AddParameter cmd)
        {
            var resolvedValue = _valueResolver.Resolve(cmd.Value, cmd.ValueType);
            if (string.IsNullOrWhiteSpace(resolvedValue)) return;

            var parameter = _doc.CreateElement("parameter");

            var name = CreateTagElem("name", cmd.Name);
            var units = CreateTagElem("units", _valueResolver.Resolve(cmd.Units, cmd.UnitsType));
            var valueTagName = cmd.IsNum ? "numeric_value" : "string_value";
            var value = CreateTagElem(valueTagName, resolvedValue);

            parameter.AppendChild(name);
            parameter.AppendChild(value);
            parameter.AppendChild(units);

            if (!string.IsNullOrWhiteSpace(cmd.Description))
            {
                var description = CreateTagElem("description", cmd.Description);
                parameter.AppendChild(description);
            }
            CurrentParent.AppendChild(parameter);
        }

        // ReSharper disable once SuggestBaseTypeForParameter
        private void Execute(AddKeywords cmd)
        {
            var keyword = _doc.CreateElement("keyword");
            var name = CreateTagElem("name", _valueResolver.Resolve(cmd.Value, cmd.ValueType));
            keyword.AppendChild(name);
            CurrentParent.AppendChild(keyword);
        }

        private XmlElement CreateTagElem(string tagName, string value)
        {
            var elem = _doc.CreateElement(tagName);
            elem.InnerText = value;
            return elem;
        }

        private XmlNode CurrentParent
        {
            get
            {
                if (_tableStack.TryPeek(out var parent))
                {
                    return parent;
                }

                return _doc;
            }
        }
    }
}
