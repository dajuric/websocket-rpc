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

using Newtonsoft.Json;
using Newtonsoft.Json.Serialization;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Threading.Tasks;

namespace WebSocketRPC
{
    /// <summary>
    /// Class providing methods for invoking the RPC.
    /// </summary>
    public static class RPC
    {
        /// <summary>
        /// Gets the all binders.
        /// </summary>
        internal static readonly List<IBinder> AllBinders = new List<IBinder>();

        #region Settings

        class CamelCaseExceptDictionaryKeysResolver : CamelCasePropertyNamesContractResolver
        {
            protected override JsonDictionaryContract CreateDictionaryContract(Type objectType)
            {
                JsonDictionaryContract contract = base.CreateDictionaryContract(objectType);

                contract.DictionaryKeyResolver = propertyName => propertyName;

                return contract;
            }
        }

        /// <summary>
        /// Gets the message serializer.
        /// </summary>
        internal static readonly JsonSerializer Serializer = JsonSerializer.Create(new JsonSerializerSettings { ContractResolver = new CamelCaseExceptDictionaryKeysResolver() });

        /// <summary>
        /// Adds the JSON converter responsible for the serialization/deserialization of the function arguments and return values (if any).
        /// </summary>
        /// <param name="converter">JSON converter.</param>
        public static void AddConverter(JsonConverter converter)
        {
            Serializer.Converters.Add(converter);
        }

        /// <summary>
        /// Gets or sets a delay after a remote call will be canceled by raising <see cref="OperationCanceledException"/>.
        /// <para>Values equal or less than zero (total milliseconds) correspond to an indefinite time period.</para>
        /// </summary>
        public static TimeSpan RpcTerminationDelay { get; set; } = TimeSpan.FromSeconds(30);

        #endregion


        #region Bind

        /// <summary>
        /// Enables incoming RPC messages to invoke methods of the provided object by creating one-way local binding. 
        /// </summary>
        /// <typeparam name="TObj">Object type.</typeparam>
        /// <param name="connection">Existing connection to bind to.</param>
        /// <param name="obj">Object which methods will be invoked remotely.</param>
        /// <returns>Local binder which associates the object and the connection.</returns>
        public static ILocalBinder<TObj> Bind<TObj>(this Connection connection, TObj obj)
        { 
            if (AllBinders.ToArray().OfType<ILocalBinder<TObj>>().Any(x => x.Connection == connection))
                throw new NotSupportedException("Only one local binder is permitted.");

            return new LocalBinder<TObj>(connection, obj);
        }

        /// <summary>
        /// Enables outgoing RPC messages to be sent using the provided connection by creating one-way remote binding.
        /// </summary>
        /// <typeparam name="TInterface">Interface type.</typeparam>
        /// <param name="connection">Existing connection to bind to.</param>
        /// <returns>Remote binder which translates .NET calls to JSON RPC messages.</returns>
        public static IRemoteBinder<TInterface> Bind<TInterface>(this Connection connection)
        {
            if (AllBinders.ToArray().OfType<IRemoteBinder<TInterface>>().Any(x => x.Connection == connection))
                throw new NotSupportedException("Only one remote binder is permitted.");

            return new RemoteBinder<TInterface>(connection, RpcTerminationDelay);
        }

        /// <summary>
        /// Enables full-duplex RPC messaging between the provided object and the remote API which has a <typeparamref name="TInterface"/> contract.
        /// <para>It is a shorthand for assigning a local and a remote binder separately.</para>
        /// </summary>
        /// <typeparam name="TObj">Object type.</typeparam>
        /// <typeparam name="TInterface">Interface type.</typeparam>
        /// <param name="connection">Existing connection to bind to.</param>
        /// <param name="obj">Object which methods will be invoked remotely.</param>
        /// <returns>Local and remote binder which associate the connection with the object and the remote interface.</returns>
        public static (ILocalBinder<TObj>, IRemoteBinder<TInterface>) Bind<TObj, TInterface>(this Connection connection, TObj obj)
        {
            return (connection.Bind(obj), connection.Bind<TInterface>());
        }

        #endregion


        #region For

        /// <summary>
        /// Gets all the binders associated with the specified connection.
        /// </summary>
        /// <param name="connection">Connection.</param>
        /// <returns>Binders associated with the connection.</returns>
        public static IEnumerable<IBinder> For(Connection connection)
        {
            return AllBinders.ToArray()
                             .Where(x => x.Connection == connection);
        }

        /// <summary>
        /// Gets all one-way remote binders.
        /// </summary>
        /// <typeparam name="TInterface">Interface type.</typeparam>
        /// <returns>Binders.</returns>
        public static IEnumerable<IRemoteBinder<TInterface>> For<TInterface>()
        {
            return AllBinders.ToArray().OfType<IRemoteBinder<TInterface>>(); 
        }

        /// <summary>
        /// Gets all remote binders which connection also has a local binder associated with the specified object.
        /// </summary>
        /// <typeparam name="TInterface">Interface type.</typeparam>
        /// <param name="obj">Target object.</param>
        /// <returns>Remote binders.</returns>
        public static IEnumerable<IRemoteBinder<TInterface>> For<TInterface>(object obj)
        {
            var lBinderType = typeof(ILocalBinder<>).MakeGenericType(obj.GetType());
            var lObjBinders = AllBinders.ToArray() // prevent 'Collection was modified'
                                        .Where(x =>
                                        {
                                            var xType = x.GetType();

                                            var isLocalBinder = lBinderType.IsAssignableFrom(xType);
                                            if (!isLocalBinder) return false;

                                            var isObjBinder = xType.GetProperty(nameof(ILocalBinder<object>.Object)).GetValue(x, null) == obj;
                                            return isObjBinder;
                                        });


            var rObjBinders = AllBinders.ToArray() // prevent 'Collection was modified'
                                        .OfType<IRemoteBinder<TInterface>>()
                                        .Where(rb => lObjBinders.Any(lb => lb.Connection == rb.Connection));

            return rObjBinders;
        }

        #endregion


        #region Call

        /// <summary>
        /// Calls the remote method.
        /// </summary>
        /// <typeparam name="TInterface">Interface type.</typeparam>
        /// <param name="binders">Remote binder collection.</param>
        /// <param name="functionExpression">Method getter.</param>
        /// <returns>RPC waiting task.</returns>
        public static async Task CallAsync<TInterface>(this IEnumerable<IRemoteBinder<TInterface>> binders, Expression<Action<TInterface>> functionExpression)
        {
            var tasks = new List<Task>();
            foreach (var b in binders)
            {
                var t = b.CallAsync(functionExpression);
                tasks.Add(t);
            }

            await Task.WhenAll(tasks);
        }

        /// <summary>
        /// Calls the remote method.
        /// </summary>
        /// <typeparam name="TInterface">Interface type.</typeparam>
        /// <typeparam name="TResult">Method result type.</typeparam>
        /// <param name="binders">Remote binder collection.</param>
        /// <param name="functionExpression">Method getter.</param>
        /// <returns>A task representing RPC waiting task. The result is the collection of return values for each called method instance.</returns>
        public static async Task<TResult[]> CallAsync<TInterface, TResult>(this IEnumerable<IRemoteBinder<TInterface>> binders, Expression<Func<TInterface, TResult>> functionExpression)
        {
            var tasks = new List<Task<TResult>>();
            foreach (var b in binders)
            {
                var t = b.CallAsync(functionExpression);
                tasks.Add(t);
            }

            await Task.WhenAll(tasks);
            var results = tasks.Where(x => x.Status == TaskStatus.RanToCompletion)
                               .Select(x => x.Result)
                               .ToArray();

            return results;
        }

        /// <summary>
        /// Calls the remote method.
        /// </summary>
        /// <typeparam name="TInterface">Interface type.</typeparam>
        /// <param name="binders">Remote binder collection.</param>
        /// <param name="functionExpression">Method getter.</param>
        /// <returns>RPC waiting task.</returns>
        public static async Task CallAsync<TInterface>(this IEnumerable<IRemoteBinder<TInterface>> binders, Expression<Func<TInterface, Task>> functionExpression)
        {
            var tasks = new List<Task>();
            foreach (var b in binders)
            {
                var t = b.CallAsync(functionExpression);
                tasks.Add(t);
            }

            await Task.WhenAll(tasks);
        }

        /// <summary>
        /// Calls the remote method.
        /// </summary>
        /// <typeparam name="TInterface">Interface type.</typeparam>
        /// <typeparam name="TResult">Method result type.</typeparam>
        /// <param name="binders">Remote binder collection.</param>
        /// <param name="functionExpression">Method getter.</param>
        /// <returns>A task representing RPC waiting task. The result is the collection of return values for each called method instance.</returns>
        public static async Task<TResult[]> CallAsync<TInterface, TResult>(this IEnumerable<IRemoteBinder<TInterface>> binders, Expression<Func<TInterface, Task<TResult>>> functionExpression)
        {
            var tasks = new List<Task<TResult>>();
            foreach (var b in binders)
            {
                var t = b.CallAsync(functionExpression);
                tasks.Add(t);
            }

            await Task.WhenAll(tasks);
            var results = tasks.Where(x => x.Status == TaskStatus.RanToCompletion)
                               .Select(x => x.Result)
                               .ToArray();

            return results;
        }

        #endregion


        #region Misc

        /// <summary>
        /// Gets whether the data contain RPC message or not.
        /// <para>Useful for filtering incoming messages if both user and RPC messages are sent/received.</para>
        /// </summary>
        /// <param name="message">Received data.</param>
        /// <returns>True if the data contains RPC message, false otherwise.</returns>
        public static bool IsRpcMessage(string message)
        {
            return !Request.FromJson(message).IsEmpty || !Response.FromJson(message).IsEmpty;
        }

        #endregion
    }
}
