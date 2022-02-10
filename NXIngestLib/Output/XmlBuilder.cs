using System.Collections.Generic;
using System.Xml;
using log4net;

namespace NXIngest.Output
{
    /// <summary>
    /// Builds an XML DOM from the values in MappingCommands before saving it as a file
    /// </summary>
    public class XmlBuilder
    {
        private readonly ILog _log = LogManager.GetLogger(typeof(XmlBuilder));

        private readonly XmlDocument _doc = new();

        // The current stack of parent tables. New elements are added as children
        // of the table on top of the stack.
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
            var value = _valueResolver.Resolve(cmd.Value, cmd.ValueType);
            if (string.IsNullOrWhiteSpace(value))
            {
                _log.Warn($"Couldn't resolve record value '{cmd.Value}', type '{cmd.ValueType}'");
                return;
            }
            var record = _doc.CreateElement(cmd.Name);
            record.InnerText = value;
            CurrentParent.AppendChild(record);
        }

        private void Execute(AddParameter cmd)
        {
            var resolvedValue = _valueResolver.Resolve(cmd.Value, cmd.ValueType);
            if (string.IsNullOrWhiteSpace(resolvedValue))
            {
                _log.Warn($"Couldn't resolve parameter value '{cmd.Value}', type '{cmd.ValueType}'");
                return;
            }

            var resolvedUnitsValue =
                _valueResolver.Resolve(cmd.Units, cmd.UnitsType);
            if (string.IsNullOrWhiteSpace(resolvedUnitsValue))
            {
                _log.Warn($"Couldn't resolve parameter units value '{cmd.Value}', type '{cmd.ValueType}'");
                return;
            }

            var parameter = _doc.CreateElement("parameter");

            var name = CreateTagElem("name", cmd.Name);
            var units = CreateTagElem("units", resolvedUnitsValue);
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
            var value = _valueResolver.Resolve(cmd.Value, cmd.ValueType);
            if (string.IsNullOrWhiteSpace(value))
            {
                _log.Warn($"Couldn't resolve keyword value '{cmd.Value}', type '{cmd.ValueType}'");
                return;
            }

            foreach (var kw in ValueProcessing.ToKeywords(value))
            {
                var keyword = _doc.CreateElement("keyword");
                var name = CreateTagElem("name", kw);
                keyword.AppendChild(name);
                CurrentParent.AppendChild(keyword);
            }
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
