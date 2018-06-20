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
        class RpcWaiter: IDisposable
        {
            TaskCompletionSource<Response> completionSource;
            CancellationTokenSource tokenSource;

            public RpcWaiter(TimeSpan delay)
            {
                var delayMs = (int)delay.TotalMilliseconds;
                if (delayMs <= 0) delayMs = -1;

                completionSource = new TaskCompletionSource<Response>();
                tokenSource = new CancellationTokenSource(delayMs);

                tokenSource.Token.Register(() =>
                {
                    var ex = new OperationCanceledException("RPC was canceled due to timeout.");
                    completionSource.TrySetException(ex);
                });
            }

            public Task<Response> Task => completionSource.Task;

            public void SetResult(Response result)
            {
                completionSource.SetResult(result);
            }

            public void Dispose()
            {
                if (tokenSource == null)
                    return;

                tokenSource.Dispose();
                tokenSource = null;
            }
        }

        #region Static

        static HashSet<Type> verifiedTypes = new HashSet<Type>();

        static void verifyType()
        {
            if (!typeof(TInterface).IsInterface)
                throw new Exception($"The specified type '{typeof(TInterface).Name}' must be an interface type.");

            var methodList = typeof(TInterface).GetMethods(BindingFlags.Public | BindingFlags.Instance);

            var propertyList = typeof(TInterface).GetProperties(BindingFlags.Public | BindingFlags.Instance);
            if (propertyList.Any())
                throw new NotSupportedException($"The interface '{typeof(TInterface).Name}' must not declare any properties: { String.Join(", ", propertyList.Select(x => x.Name)) }.");
        }

        #endregion

        Func<Request, Task> sendAsync;
        ConcurrentDictionary<string, RpcWaiter> runningMethods;

        public RemoteInvoker(TimeSpan rpcTerminationDelay)
        {
            RequestTerminationDelay = rpcTerminationDelay;
            runningMethods = new ConcurrentDictionary<string, RpcWaiter>();
            if (verifiedTypes.Contains(typeof(TInterface))) return;

            //verify constraints
            verifyType();
            //cache it
            verifiedTypes.Add(typeof(TInterface));  
        }

        public void Initialize(Func<Request, Task> sendAsync)
        {
            this.sendAsync = sendAsync;
        }

        public void Receive(Response response)
        {
            var key = response.FunctionName + "-" + response.CallId;

            lock (runningMethods)
            {
                if (runningMethods.ContainsKey(key))
                    runningMethods[key].SetResult(response);
            }
        }

        public TimeSpan RequestTerminationDelay { get; private set; }

        #region Invoke

        public async Task InvokeAsync(Expression<Action<TInterface>> functionExpression)
        {
            var (funcName, argVals) = getFunctionInfo(functionExpression);
            var response = await invokeAsync(funcName, argVals);

            if (response.Error != null)
                throw new Exception($"Exception thrown while calling {funcName}. Message: {response.Error}.");
        }

        public async Task<TResult> InvokeAsync<TResult>(Expression<Func<TInterface, TResult>> functionExpression)
        {
            var (funcName, argVals) = getFunctionInfo(functionExpression);
            var response = await invokeAsync(funcName, argVals);

            if (response.Error != null)
                throw new Exception($"Exception thrown while calling {funcName}. Message: {response.Error}.");

            var result = response.ReturnValue.ToObject<TResult>(RPC.Serializer);
            return result;
        }

        public async Task InvokeAsync(Expression<Func<TInterface, Task>> functionExpression)
        {
            var (funcName, argVals) = getFunctionInfo(functionExpression);
            var response = await invokeAsync(funcName, argVals);

            if (response.Error != null)
                throw new Exception($"Exception thrown while calling {funcName}. Message: {response.Error}.");
        }

        public async Task<TResult> InvokeAsync<TResult>(Expression<Func<TInterface, Task<TResult>>> functionExpression)
        {
            var (funcName, argVals) = getFunctionInfo(functionExpression);
            var response = await invokeAsync(funcName, argVals);

            if (response.Error != null)
                throw new Exception($"Exception thrown while calling {funcName}. Message: {response.Error}.");

            var result = response.ReturnValue.ToObject<TResult>(RPC.Serializer);
            return result;
        }

        async Task<Response> invokeAsync(string name, params object[] args)
        {
            if (sendAsync == null)
                throw new Exception("The invoker is not initialized.");

            var msg = new Request
            {
                FunctionName = name,
                CallId = Guid.NewGuid().ToString(),
                Arguments = args.Select(a => JToken.FromObject(a, RPC.Serializer)).ToArray()
            };

            var key = msg.FunctionName + "-" + msg.CallId;
            runningMethods[key] = new RpcWaiter(RequestTerminationDelay);
            await sendAsync(msg);

            Response response = default(Response);
            try
            {
                await runningMethods[key].Task;
                response = runningMethods[key].Task.Result;
            }
            //catch (OperationCanceledException) { throw; }
            finally
            {
                runningMethods[key].Dispose();
                runningMethods.TryRemove(key, out RpcWaiter _);
            }

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

            var fName = call.Method.Name;
            var type = call.Method.DeclaringType;
            
            return ($"{type.FullName}.{fName}", values.ToArray());
        }

        #endregion
    }
}
