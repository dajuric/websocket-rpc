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
using System.Linq.Expressions;
using System.Text;
using System.Threading.Tasks;

namespace WebsocketRPC
{
    /// <summary>
    /// Provides methods for invoking the RPC.
    /// </summary>
    public static class RPC
    {
        /// <summary>
        /// Gets the all binders.
        /// </summary>
        public static readonly List<IBinder> AllBinders = new List<IBinder>();

        /// <summary>
        /// Creates two-way RPC receiving-sending binding for the provided connection.
        /// </summary>
        /// <typeparam name="TObj">Object type.</typeparam>
        /// <typeparam name="TInterface">Interface type.</typeparam>
        /// <param name="connection">Existing connection to bind to.</param>
        /// <param name="obj">Object to bind to.</param>
        /// <returns>Binder.</returns>
        public static IBinder Bind<TObj, TInterface>(this Connection connection, TObj obj)
        {
            return new Binder<TObj, TInterface>(connection, obj);
        }

        /// <summary>
        /// Creates one way RPC receiving binding for the provided connection.
        /// </summary>
        /// <typeparam name="TObj">Object type.</typeparam>
        /// <param name="connection">Existing connection to bind to.</param>
        /// <param name="obj">Object to bind to.</param>
        /// <returns>Binder.</returns>
        public static IBinder Bind<TObj>(this Connection connection, TObj obj)
        {
            return new LocalBinder<TObj>(connection, obj);
        }

        /// <summary>
        /// Creates one way RPC sending binding for the provided connection.
        /// </summary>
        /// <typeparam name="TInterface">Interface type.</typeparam>
        /// <param name="connection">Existing connection to bind to.</param>
        /// <returns>Binder.</returns>
        public static IBinder Bind<TInterface>(this Connection connection)
        {
            return new RemoteBinder<TInterface>(connection);
        }

        /// <summary>
        /// Gets all two-way or one-way sending binders.
        /// </summary>
        /// <typeparam name="TInterface">Interface type.</typeparam>
        /// <returns>Binders.</returns>
        public static IEnumerable<IRemoteBinder<TInterface>> For<TInterface>()
        {
            return AllBinders.OfType<IRemoteBinder<TInterface>>(); 
        }

        /// <summary>
        /// Gets all two-way binders associated with the specified object.
        /// </summary>
        /// <typeparam name="TInterface">Interface type.</typeparam>
        /// <param name="obj">Target object.</param>
        /// <returns>Binders.</returns>
        public static IEnumerable<IRemoteBinder<TInterface>> For<TInterface>(object obj)
        {
            var lBinderType = typeof(ILocalBinder<>).MakeGenericType(obj.GetType());

            var binders = AllBinders.OfType<IRemoteBinder<TInterface>>()
                                    .Where(x =>
                                    {
                                        var xType = x.GetType();

                                        var isLocalBinder = lBinderType.IsAssignableFrom(xType);
                                        if (!isLocalBinder) return false;

                                        var isObjBinder = xType.GetProperty(nameof(ILocalBinder<object>.Object)).GetValue(x, null) == obj;
                                        return isObjBinder;
                                    });

            return binders;
        }

        /// <summary>
        /// Calls the remote method.
        /// </summary>
        /// <typeparam name="TInterface">Interface type.</typeparam>
        /// <typeparam name="TResult">Method result type.</typeparam>
        /// <param name="binders">Remote binder collection.</param>
        /// <param name="functionExpression">Method getter.</param>
        /// <returns>The collection of the RPC invoking tasks.</returns>
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
        /// Gets whether the data contain RPC message or not.
        /// </summary>
        /// <param name="data">Received data.</param>
        /// <returns>True if the data contain RPC message, false otherwise.</returns>
        public static bool IsRPC(this ArraySegment<byte> data)
        {
            var str = data.ToString(Encoding.ASCII);
            return !Request.FromJson(str).IsEmpty || !Response.FromJson(str).IsEmpty;
        }
    }
}
