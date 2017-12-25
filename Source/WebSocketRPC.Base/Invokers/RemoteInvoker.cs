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
using System.Linq;
using Newtonsoft.Json.Linq;
using System.Threading.Tasks;
using System.Linq.Expressions;
using System.Reflection;
using System.Collections.Concurrent;
using System.Threading;

namespace WebSocketRPC
{
    class RemoteInvoker<TInterface>
    {
        static HashSet<Type> verifiedTypes = new HashSet<Type>();

        Func<Request, Task> sendAsync;
        ConcurrentDictionary<string, TaskCompletionSource<Response>> runningMethods;
        ConcurrentDictionary<string, SemaphoreSlim> methodConcurencySyncs;

        public RemoteInvoker()
        {
            runningMethods = new ConcurrentDictionary<string, TaskCompletionSource<Response>>();
            methodConcurencySyncs = new ConcurrentDictionary<string, SemaphoreSlim>();
            if (verifiedTypes.Contains(typeof(TInterface))) return;

            //verify constraints
            verifyType();
            //cache it
            verifiedTypes.Add(typeof(TInterface));  
        }

        static void verifyType()
        {
            if (!typeof(TInterface).IsInterface)
                throw new Exception("The specified type must be an interface type.");

            var methodList = typeof(TInterface).GetMethods(BindingFlags.Public | BindingFlags.Instance);

            var propertyList = typeof(TInterface).GetProperties(BindingFlags.Public | BindingFlags.Instance);
            if (propertyList.Any())
                throw new NotSupportedException("The interface must not declare any properties: " + String.Join(", ", propertyList.Select(x => x.Name)) + ".");
        }


        public void Initialize(Func<Request, Task> sendAsync)
        {
            this.sendAsync = sendAsync;
        }

        public void Receive(Response response)
        {
            var key = response.FunctionName + "-" + response.CallIndex;

            lock (runningMethods)
            {
                if (runningMethods.ContainsKey(key))
                    runningMethods[key].SetResult(response);
            }
        }


        public async Task InvokeAsync(Expression<Action<TInterface>> functionExpression)
        {
            var (funcName, argVals) = getFunctionInfo(functionExpression);
            var response = await invokeAsync(funcName, argVals);

            if (response.Error != null)
                throw new Exception(response.Error);
        }

        public async Task<TResult> InvokeAsync<TResult>(Expression<Func<TInterface, TResult>> functionExpression)
        {
            var (funcName, argVals) = getFunctionInfo(functionExpression);
            var response = await invokeAsync(funcName, argVals);

            var result = response.ReturnValue.ToObject<TResult>(RPCSettings.Serializer);
            if (response.Error != null)
                throw new Exception(response.Error);

            return result;
        }

        public async Task InvokeAsync(Expression<Func<TInterface, Task>> functionExpression)
        {
            var (funcName, argVals) = getFunctionInfo(functionExpression);
            var response = await invokeAsync(funcName, argVals);

            if (response.Error != null)
                throw new Exception(response.Error);
        }

        public async Task<TResult> InvokeAsync<TResult>(Expression<Func<TInterface, Task<TResult>>> functionExpression)
        {
            var (funcName, argVals) = getFunctionInfo(functionExpression);
            var response = await invokeAsync(funcName, argVals);

            var result = response.ReturnValue.ToObject<TResult>(RPCSettings.Serializer);
            if (response.Error != null)
                throw new Exception(response.Error);

            return result;
        }

        ConcurrentDictionary<string, int> callIndex = new ConcurrentDictionary<string, int>();
        async Task<Response> invokeAsync(string name, params object[] args)
        {
            if (sendAsync == null)
                throw new Exception("The invoker is not initialized.");

            //Console.WriteLine("Queue: " + name + " Task: " + Thread.CurrentThread.ManagedThreadId);
            //methodConcurencySyncs.GetOrAdd(name, new SemaphoreSlim(1));
            //await methodConcurencySyncs[name].WaitAsync(); //wait for the previous task (functions with the same name are run sequentially)

            var msg = new Request
            {
                FunctionName = name,
                CallIndex = callIndex.AddOrUpdate(name, 0, (k, v) => v + 1),
                Arguments = args.Select(a => JToken.FromObject(a, RPCSettings.Serializer)).ToArray()
            };

            //Console.WriteLine("Invoking: " + name + " Task: " + Thread.CurrentThread.ManagedThreadId);
            var key = msg.FunctionName + "-" + msg.CallIndex;

            runningMethods[key] = new TaskCompletionSource<Response>();
            await sendAsync(msg);
            await runningMethods[key].Task;

            var response = runningMethods[key].Task.Result;
            runningMethods.TryRemove(key, out TaskCompletionSource<Response> _);

            //Console.WriteLine("End invoke: " + name + " Task: " + Thread.CurrentThread.ManagedThreadId);

            key = msg.FunctionName + "-" + msg.CallIndex;
            callIndex.TryRemove(key, out int _);
            //methodConcurencySyncs[name].Release();
            return response;
        }

        //the idea is taken from: https://stackoverflow.com/questions/3766698/get-end-values-from-lambda-expressions-method-parameters?rq=1
        static (string fName, object[] argVals) getFunctionInfo<T>(Expression<T> expression)
        {
            var call = expression.Body as MethodCallExpression;
            if (call == null)
                throw new ArgumentException("Not a method call: " + expression.Name);

            var values = new List<object>();
            foreach (var argument in call.Arguments)
            {
                var lambda = Expression.Lambda(argument, expression.Parameters);
                var value = lambda.Compile().DynamicInvoke(new object[1]);

                values.Add(value);
            }

            var fName = ((MethodCallExpression)expression.Body).Method.Name;
            return (fName, values.ToArray());
        }
    }
}
