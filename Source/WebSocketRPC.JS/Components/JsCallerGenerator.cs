#region License
// Copyright © 2017 Darko Jurić
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
using System.Linq.Expressions;
using System.Reflection;
using System.Text;

namespace WebSocketRPC
{
    static class JsCallerGenerator
    {
        public static (string, MethodInfo[]) GetMethods<T>(params Expression<Action<T>>[] omittedMethods)
        {
            //get omitted methods
            var omittedMethodNames = new HashSet<string>();
            foreach (var oM in omittedMethods)
            {
                var mInfo = (oM.Body as MethodCallExpression)?.Method;
                if (mInfo == null)
                    continue;

                omittedMethodNames.Add(mInfo.Name);
            }

            //get methods
            var objType = typeof(T);
            var methodList = objType.GetMethods(BindingFlags.Public | BindingFlags.Instance | BindingFlags.DeclaredOnly);

            var overloadedMethodNames = methodList.GroupBy(x => x.Name)
                                                  .DefaultIfEmpty()
                                                  .Where(x => x.Count() > 1)
                                                  .Select(x => x.Key);

            if (overloadedMethodNames.Any())
                throw new NotSupportedException("Overloaded functions are not supported: " + String.Join(", ", overloadedMethodNames));


            methodList = methodList.Where(x => !omittedMethodNames.Contains(x.Name))
                                   .ToArray();

            return (objType.Name, methodList);
        }

        public static string GenerateRequireJsHeader(string className)
        {
            var t = new string[] {
                $"define(() => {className}); //'require.js' support"
            };

            var sb = new StringBuilder();
            sb.Append(String.Join(Environment.NewLine, t));
            sb.Append(Environment.NewLine);
            sb.Append(Environment.NewLine);

            return sb.ToString();
        }

        public static string GenerateHeader(string className)
        {
            var t = new string[] {
                $"function {className}(url)",
                $"{{"
            };

            var sb = new StringBuilder();
            sb.Append(String.Join(Environment.NewLine, t));
            sb.Append(Environment.NewLine);

            //API base
            var assembly = Assembly.GetExecutingAssembly();
            var resourceName = assembly.GetManifestResourceNames().First(x => x.Contains("ClientAPIBase.js"));

            var lines = new List<string>();
            using (Stream stream = assembly.GetManifestResourceStream(resourceName))
            using (StreamReader reader = new StreamReader(stream))
            {
                string l = null;
                while ((l = reader.ReadLine()) != null)
                    lines.Add(l);
            }

            var apiBase = String.Join(Environment.NewLine, lines.Select(x => "\t" + x));
            sb.Append(apiBase);
            sb.Append(Environment.NewLine);

            return sb.ToString();
        }

        public static string GenerateMethod(string methodName, string[] argNames)
        {
            var jsMName = Char.ToLower(methodName.First()) + methodName.Substring(1);
            var argList = String.Join(", ", argNames);

            var t = new string[] {
                $"\t this.{jsMName} = function({argList}) {{",
                $"\t    return callRPC(\"{methodName}\", Object.values(this.{jsMName}.arguments));",
                $"\t }};",
                $"{Environment.NewLine}"
            };

            var sb = new StringBuilder();
            sb.Append(String.Join(Environment.NewLine, t));

            return sb.ToString();
        }

        public static string GenerateFooter()
        {
            var t = new string[] {
                $"}}"
            };

            var sb = new StringBuilder();
            sb.Append(String.Join(Environment.NewLine, t));

            return sb.ToString();
        }
    }
}
