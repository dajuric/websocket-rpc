#region License
// Copyright © 2018 Darko Jurić
//
// Permission is hereby granted, free of charge, to any person
// obtaining a copy of this software and associated documentation
// files (the "Software"), to deal in the Software without
// restriction, including without limitation the rights to use,
// copy, modify, merge, publish, distribute, sublicense, and/or sell
// copies of the Software, and to permit persons to whom the
// Software is furnished to do so, subject to the following
// conditions:
//
// The above copyright notice and this permission notice shall be
// included in all copies or substantial portions of the Software.
//
// THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND,
// EXPRESS OR IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES
// OF MERCHANTABILITY, FITNESS FOR A PARTICULAR PURPOSE AND
// NONINFRINGEMENT. IN NO EVENT SHALL THE AUTHORS OR COPYRIGHT
// HOLDERS BE LIABLE FOR ANY CLAIM, DAMAGES OR OTHER LIABILITY,
// WHETHER IN AN ACTION OF CONTRACT, TORT OR OTHERWISE, ARISING
// FROM, OUT OF OR IN CONNECTION WITH THE SOFTWARE OR THE USE OR
// OTHER DEALINGS IN THE SOFTWARE.
#endregion

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Xml;

namespace WebSocketRPC
{
    static class JsDocGenerator
    {
        public static XmlNodeList GetMemberNodes(string xmlDocPath)
        {
            if (!File.Exists(xmlDocPath))
                throw new FileNotFoundException("The provided xml-doc path points to a non-existent file.");

            var xmlDoc = new XmlDocument();
            xmlDoc.Load(xmlDocPath);

            var mElems = xmlDoc.GetElementsByTagName("members");
            if (mElems.Count != 1)
                return mElems; //as I can not construct empty list

            return mElems[0].ChildNodes;
        }

        public static string GetClassDoc(XmlNodeList members, string className)
        {
            string s = null;
            for (int eIdx = 0; eIdx < members.Count; eIdx++)
            {
                var n = members[eIdx];
                if (n.Name != "member" || (bool)n.Attributes["name"]?.Value?.Contains(className) == false)
                    continue;

                s = n.InnerText.Trim('\r', '\n', ' ');
                break;
            }

            if (s == null)
                return String.Empty;

            var jsDoc = new StringBuilder();
            jsDoc.AppendLine("/**");
            jsDoc.AppendLine(String.Format(" * {0}", s));
            jsDoc.AppendLine(" * @constructor");
            jsDoc.AppendLine("*/");

            return jsDoc.ToString();
        }

        public static string GetMethodDoc(XmlNodeList mmebers, string methodName, 
                                          IList<string> pNames, IList<Type> pTypes, Type returnType, string linePrefix = "\t")
        {
            var mElem = getMethod(mmebers, methodName);
            if (mElem == null) return String.Empty;

            var s = getSummary(mElem);
            var p = getParams(mElem);
            var r = getReturn(mElem);

            var jsDoc = new StringBuilder();
            jsDoc.AppendLine(String.Format("{0}/**", linePrefix));
            {
                jsDoc.AppendLine(String.Format("{0} * @description - {1}", linePrefix, s));
                jsDoc.AppendLine(String.Format("{0} *", linePrefix));

                for (int i = 0; i < pNames.Count; i++)
                {
                    if (!p.ContainsKey(pNames[i]))
                        continue;

                    jsDoc.AppendLine(String.Format("{0} * @param {{{1}}} - {2}", linePrefix, pTypes[i].Name, p[pNames[i]]));
                }

                jsDoc.AppendLine(String.Format("{0} * @returns {{{1}}} - {2}", linePrefix, getTypeName(returnType), r));
            }
            jsDoc.AppendLine(String.Format("{0}*/", linePrefix));

            return jsDoc.ToString();
        }

        static XmlNode getMethod(XmlNodeList nodes, string mName)
        {
            for (int eIdx = 0; eIdx < nodes.Count; eIdx++)
            {
                var n = nodes[eIdx];
                if (n.Name != "member" || (bool)n.Attributes["name"]?.Value?.Contains(mName) == false)
                    continue;

                return n;
            }

            return null;
        }

        static string getSummary(XmlNode node)
        {
            string s = String.Empty;

            for (int eIdx = 0; eIdx < node.ChildNodes.Count; eIdx++)
            {
                var n = node.ChildNodes[eIdx];
                if (n.Name != "summary")
                    continue;

                s = n.InnerText.Trim('\r', '\n', ' ');
                break;
            }

            return s;
        }

        static Dictionary<string, string> getParams(XmlNode node)
        {
            var pInfos = new Dictionary<string, string>();

            for (int eIdx = 0; eIdx < node.ChildNodes.Count; eIdx++)
            {
                var n = node.ChildNodes[eIdx];

                if (n.Name != "param")
                    continue;

                var pName = n.Attributes["name"]?.Value;
                var pDesc = n.InnerText;

                pInfos.Add(pName, pDesc);
            }

            return pInfos;
        }

        static string getReturn(XmlNode node)
        {
            string s = String.Empty;

            for (int eIdx = 0; eIdx < node.ChildNodes.Count; eIdx++)
            {
                var n = node.ChildNodes[eIdx];
                if (n.Name != "returns")
                    continue;

                s = n.InnerText.Trim('\r', '\n', ' ');
                break;
            }

            return s;
        }

        static string getTypeName(Type type)
        {
            if (!type.IsGenericType || type.GetGenericTypeDefinition() != typeof(Task<>))
                return type.Name;

            return type.GenericTypeArguments.First().Name + " (Task)";
        }
    }
}
