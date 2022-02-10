using System.Collections.Generic;
using System.Xml;

namespace NXIngest.Mapping
{
    public static class XmlExtensions
    {
        public static IEnumerable<XmlNode> Children(this XmlNode node)
        {
            var children = node.ChildNodes;
            for (var i = 0; i < children.Count; i++)
            {
                yield return children[i];
            }
        }

        public static IEnumerable<XmlNode> AttributeItems(this XmlNode node)
        {
            if (node.Attributes == null) yield break;

            var attrs = node.Attributes;
            for (var i = 0; i < attrs.Count; i++)
            {
                yield return attrs[i];
            }
        }
    }
}
