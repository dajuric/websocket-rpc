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
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;

namespace WebSocketRPC
{
    /// <summary>
    /// Contains function related to Javascript RPC.
    /// </summary>
    public static class RPCJs
    {
        /// <summary>
        /// Generates Javascript code from the provided class or interface type.
        /// </summary>
        /// <typeparam name="T">Class or interface type.</typeparam>
        /// <param name="settings">RPC-Js settings used for Javascript code generation.</param>
        /// <returns>Javascript API.</returns>
        public static string GenerateCaller<T>(RPCJsSettings<T> settings =  null)
        {
            settings = settings ?? new RPCJsSettings<T>();
            var (tName, mInfos) = JsCallerGenerator.GetMethods(settings.OmittedMethods);
            tName = settings.NameOverwrite ?? tName;

            var sb = new StringBuilder();
            if(settings.WithRequireSupport) sb.Append(JsCallerGenerator.GenerateRequireJsHeader(tName));
            sb.Append(JsCallerGenerator.GenerateHeader(tName));

            foreach (var m in mInfos)
            {
                var mName = m.Name;
                var pNames = m.GetParameters().Select(x => x.Name).ToArray();

                sb.Append(JsCallerGenerator.GenerateMethod(mName, pNames));
            }

            sb.Append(JsCallerGenerator.GenerateFooter());
            return sb.ToString();
        }

        /// <summary>
        /// Generates Javascript code including JsDoc comments from the provided class or interface type.
        /// </summary>
        /// <typeparam name="T">Class or interface type.</typeparam>
        /// <param name="xmlDocPath">Xml assembly definition file.</param>
        /// <param name="settings">RPC-Js settings used for Javascript code generation.</param>
        /// <returns>Javascript API.</returns>
        public static string GenerateCallerWithDoc<T>(string xmlDocPath, RPCJsSettings<T> settings = null)
        {
            settings = settings ?? new RPCJsSettings<T>();
            var (tName, mInfos) = JsCallerGenerator.GetMethods(settings.OmittedMethods);
            tName = settings.NameOverwrite ?? tName;

            var xmlMemberNodes = JsDocGenerator.GetMemberNodes(xmlDocPath);

            var sb = new StringBuilder();
            if (settings.WithRequireSupport) sb.Append(JsCallerGenerator.GenerateRequireJsHeader(tName));
            sb.Append(JsDocGenerator.GetClassDoc(xmlMemberNodes, tName));
            sb.Append(JsCallerGenerator.GenerateHeader(tName));

            foreach (var m in mInfos)
            {
                var mName = m.Name;
                var pTypes = m.GetParameters().Select(x => x.ParameterType).ToArray();
                var pNames = m.GetParameters().Select(x => x.Name).ToArray();

                sb.Append(JsDocGenerator.GetMethodDoc(xmlMemberNodes, mName, pNames, pTypes, m.ReturnType));
                sb.Append(JsCallerGenerator.GenerateMethod(mName, pNames));
            }

            sb.Append(JsCallerGenerator.GenerateFooter());
            return sb.ToString();
        }

        /// <summary>
        /// Generates Javascript code including JsDoc comments from the provided class or interface type.
        /// <para>The xml assembly definition is taken form the executing assembly if available.</para>
        /// </summary>
        /// <typeparam name="T">Class or interface type.</typeparam>
        /// <param name="settings">RPC-Js settings used for Javascript code generation.</param>
        /// <returns>Javascript API.</returns>
        public static string GenerateCallerWithDoc<T>(RPCJsSettings<T> settings = null)
        {
            var assembly = Assembly.GetEntryAssembly();
            var fInfo = new FileInfo(assembly.Location);

            var xmlDocPath = Path.ChangeExtension(assembly.Location, ".xml");

            if (!File.Exists(xmlDocPath))
                return GenerateCaller(settings);
            else
                return GenerateCallerWithDoc(xmlDocPath, settings);
        }
    }
}
