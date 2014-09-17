using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Xml;

namespace Abstracta.FiddlerSessionComparer.Content
{
    public class XmlContentType
    {
        public XmlContentType(string name)
        {
            TagName = name;
            Attributes = new Dictionary<string, string>();
            Children = new List<XmlContentType>();
        }

        public string TagName { get; private set; }

        public string Value { get; private set; }

        public Dictionary<string, string> Attributes { get; private set; }

        public List<XmlContentType> Children { get; private set; }

        public XmlContentType Parent { get; private set; }

        public void AddChildren(XmlContentType childNode)
        {
            childNode.Parent = this;
            Children.Add(childNode);
        }

        public void AddAttribute(string name, string value)
        {
            Attributes.Add(name, value);
        }

        public void SetValue(string value)
        {
            // todo: check if it's just string or if it's Complex value
            Value = value;
        }

        public override string ToString()
        {
            var attrs = string.Join("' , '", Attributes.Select(a => a.Key + "=" + a.Value).ToArray());
            var children = string.Join("','", Children.Select(p => p.TagName).ToArray());

            return "{ " +
                   "TagName='" + TagName + "' " +
                   "Value='" + Value + "' " +
                   "Attributes=['" + attrs + "'] " +
                   "Children='" + children + "' " +
                   "}";
        }

        public static XmlContentType Deserialize(string xmlString)
        {
            XmlContentType root = null;
            XmlContentType current = null;
            XmlContentType tmp = null;

            using (var reader = XmlReader.Create(new StringReader(xmlString)))
            {
                while (reader.Read())
                {
                    switch (reader.NodeType)
                    {
                        case XmlNodeType.Element:
                            tmp = new XmlContentType(reader.Name);
                            if (reader.HasAttributes)
                            {
                                while (reader.MoveToNextAttribute())
                                {
                                    tmp.AddAttribute(reader.Name, reader.Value);
                                }

                                // Move the reader back to the element node.
                                reader.MoveToElement();
                            }

                            if (root == null)
                            {
                                root = current = tmp;
                            }
                            else
                            {
                                current.AddChildren(tmp);
                                current = tmp;
                            }

                            break;

                        case XmlNodeType.Text:
                            if (tmp == null)
                            {
                                throw new Exception("XMLContentType: ERRORCODE 1");
                            }

                            tmp.SetValue(reader.Value);
                            break;

                        case XmlNodeType.EndElement:
                            if (current == null)
                            {
                                throw new Exception("XMLContentType: ERRORCODE 2");
                            }

                            current = current.Parent;
                            break;
                    }
                }

            }

            return root;
        }

        public IEnumerable<Tuple<string, string>> GetLeaves()
        {
            var result = Attributes.Select(attribute => new Tuple<string, string>(attribute.Key, attribute.Value)).ToList();

            if (!string.IsNullOrEmpty(Value))
            {
                result.Add(new Tuple<string, string>(TagName, Value));
            }
            else 
            {
                foreach (var child in Children)
                {
                    result.AddRange(child.GetLeaves());
                }
            }

            return result;
        }
    }
}