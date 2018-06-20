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
using System.Linq;
using System.Reflection;
using System.Runtime.CompilerServices;
using System.Threading.Tasks;
using Newtonsoft.Json.Linq;
using NameInfoPairs = System.Collections.Generic.Dictionary<string, System.Reflection.MethodInfo>;

namespace WebSocketRPC
{
    class LocalInvoker<TObj>
    {
        static NameInfoPairs methods;

        static LocalInvoker()
        {
            List<MethodInfo> methodList = new List<MethodInfo>();
            methodList.AddRange(typeof(TObj).GetMethods(BindingFlags.Public | BindingFlags.Instance | BindingFlags.FlattenHierarchy));
            if (typeof(TObj).IsInterface)
            {                
                methodList.AddRange(typeof(TObj).GetInterfaces().SelectMany(t => t.GetMethods(BindingFlags.Public | BindingFlags.Instance)));
            }

            //check constrains
            verifyType(methodList);

            //initialize and cache it
            methods = methodList.ToDictionary(x => x.Name, x => x);
        }

        static void verifyType(IEnumerable<MethodInfo> methodList)
        {
            //check constraints
            //I don't see any reason as to why interfaces should not be allowed
            //if (typeof(TObj).IsInterface)
            //    throw new Exception("The specified type must be a class or struct.");

            var overloadedMethodNames = methodList.GroupBy(x => x.Name)
                                                  .DefaultIfEmpty()
                                                  .Where(x => x.Count() > 1)
                                                  .Select(x => x.Key);

            if (overloadedMethodNames.Any())
                throw new NotSupportedException("Overloaded functions are not supported: " + String.Join(", ", overloadedMethodNames));

            var propertyList = typeof(TObj).GetProperties(BindingFlags.Public | BindingFlags.Instance);
            if (propertyList.Any())
                throw new NotSupportedException("Properties are not permitted: " + String.Join(", ", propertyList.Select(x => x.Name)) + ".");
        }

        public bool CanInvoke(Request clientMessage)
        {
            var functionName = clientMessage.FunctionName;
            var (iface, name) = parseFunctionName(functionName);
            functionName = name;

            if (!interfaceMatches(iface) || !methods.ContainsKey(functionName))
                return false;

            var methodParams = methods[functionName].GetParameters();
            if (methodParams.Length != clientMessage.Arguments.Length)
                return false;

            return true;
        }

        private bool interfaceMatches(string interfaceName)
        {
            if (interfaceName == null)
                return true;

            if (typeof(TObj).GetInterfaces().Where(t => t.FullName == interfaceName).Any())
                return true;

            Type type = typeof(TObj);
            while (type != null)
            {
                if (type.FullName == interfaceName)
                    return true;
                type = type.BaseType;
            }

            return false;
        }

        public async Task<Response> InvokeAsync(TObj obj, Request clientMessage)
        {
            JToken result = null;
            Exception error = null;

            try
            {
                result = await invokeAsync(obj, clientMessage.FunctionName, clientMessage.Arguments);
            }
            catch (Exception ex)
            {
                while (ex.InnerException != null) ex = ex.InnerException;
                error = ex;
            }

            return new Response
            {
                FunctionName = clientMessage.FunctionName,
                CallId = clientMessage.CallId,
                ReturnValue = result,
                Error = error?.Message
            };
        }

        async Task<JToken> invokeAsync(TObj obj, string functionName, JToken[] args)
        {
            var (iface, name) = parseFunctionName(functionName);
            functionName = name;

            if (!interfaceMatches(iface) || !methods.ContainsKey(functionName))
                throw new ArgumentException(functionName + ": The object does not contain the provided method name: " + functionName + ".");

            var methodParams = methods[functionName].GetParameters();
            if (methodParams.Length != args.Length)
                throw new ArgumentException(functionName + ": The number of provided parameters mismatches the number of required arguments.");

            var argObjs = new object[args.Length];
            for (int i = 0; i < methodParams.Length; i++)
                argObjs[i] = args[i].ToObject(methodParams[i].ParameterType, RPC.Serializer);


            var hasResult = methods[functionName].ReturnType != typeof(void) &&
                            methods[functionName].ReturnType != typeof(Task);

            JToken result = null;
            if (hasResult)
            {
                var returnVal = await invokeWithResultAsync(methods[functionName], obj, argObjs);
                result = (returnVal != null) ? JToken.FromObject(returnVal, RPC.Serializer) : null;
            }
            else
            {
                await invokeAsync(methods[functionName], obj, argObjs);
                result = JToken.FromObject(true);
            }

            return result;
        }

        private (string iface, string name) parseFunctionName(string functionName)
        {
            int index = functionName.LastIndexOf('.');
            if (index == -1)
                return (null, functionName);

            var iface = functionName.Substring(0, index);
            var name = functionName.Substring(index + 1, functionName.Length - index - 1);
            return (iface, name);            
        }

        async Task invokeAsync(MethodInfo method, TObj obj, object[] args)
        {
            object returnVal = method.Invoke(obj, args);

            //async method support
            if (method.GetCustomAttribute<AsyncStateMachineAttribute>() != null)
            {
                var task = (Task)returnVal;
                await task.ConfigureAwait(false);
            }
        }

        async Task<object> invokeWithResultAsync(MethodInfo method, TObj obj, object[] args)
        {
            object returnVal = method.Invoke(obj, args);

            //async method support
            if (method.GetCustomAttribute<AsyncStateMachineAttribute>() != null)
            {
                var task = (Task)returnVal;
                await task.ConfigureAwait(false);

                var resultProperty = task.GetType().GetProperty("Result");
                returnVal = resultProperty.GetValue(task);
            }

            return returnVal;
        }


    }
}
