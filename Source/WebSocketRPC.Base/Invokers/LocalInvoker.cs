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
        static Dictionary<Type, NameInfoPairs> cache = new Dictionary<Type, NameInfoPairs>();
        NameInfoPairs methods;

        public LocalInvoker()
        {
            lock(cache) cache.TryGetValue(typeof(TObj), out methods);
            if (methods != null) return;

            var methodList = typeof(TObj).GetMethods(BindingFlags.Public | BindingFlags.Instance);

            //check constrains
            verifyType(methodList);

            //initialize and cache it
            methods = methodList.ToDictionary(x => x.Name, x => x);
            lock(cache) cache[typeof(TObj)] = methods;
        }

        static void verifyType(MethodInfo[] methodList)
        {
            //check constraints
            if (typeof(TObj).IsInterface)
                throw new Exception("The specified type must be a class or struct.");

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
            if (!methods.ContainsKey(functionName))
                throw new ArgumentException(functionName + ": The object does not contain the provided method name: " + functionName + ".");

            var methodParams = methods[functionName].GetParameters();
            if (methodParams.Length != args.Length)
                throw new ArgumentException(functionName + ": The number of provided parameters mismatches the number of required arguments.");

            var argObjs = new object[args.Length];
            for (int i = 0; i < methodParams.Length; i++)
                argObjs[i] = args[i].ToObject(methodParams[i].ParameterType, RPCSettings.Serializer);


            var hasResult = methods[functionName].ReturnType != typeof(void) &&
                            methods[functionName].ReturnType != typeof(Task);

            JToken result = null;
            if (hasResult)
            {
                var returnVal = await invokeWithResultAsync(methods[functionName], obj, argObjs);
                result = (returnVal != null) ? JToken.FromObject(returnVal, RPCSettings.Serializer) : null;
            }
            else
            {
                await invokeAsync(methods[functionName], obj, argObjs);
                result = JToken.FromObject(true);
            }

            return result;
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
