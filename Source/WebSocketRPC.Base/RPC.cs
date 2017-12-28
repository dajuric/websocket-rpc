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

namespace WebSocketRPC
{
    /// <summary>
    /// Provides methods for invoking the RPC.
    /// </summary>
    public static class RPC
    {
        /// <summary>
        /// Gets the all binders.
        /// </summary>
        internal static readonly List<IBinder> AllBinders = new List<IBinder>();

        #region Bind

        /// <summary>
        /// Creates one way RPC receiving binding for the provided connection.
        /// </summary>
        /// <typeparam name="TObj">Object type.</typeparam>
        /// <param name="connection">Existing connection to bind to.</param>
        /// <param name="obj">Object to bind to.</param>
        /// <returns>Binder.</returns>
        public static ILocalBinder<TObj> Bind<TObj>(this Connection connection, TObj obj)
        { 
            if (AllBinders.ToArray().OfType<ILocalBinder<TObj>>().Any(x => x.Connection == connection))
                throw new NotSupportedException("Only one local binder is permitted.");

            return new LocalBinder<TObj>(connection, obj);
        }

        /// <summary>
        /// Creates one way RPC sending binding for the provided connection.
        /// </summary>
        /// <typeparam name="TInterface">Interface type.</typeparam>
        /// <param name="connection">Existing connection to bind to.</param>
        /// <returns>Binder.</returns>
        public static IRemoteBinder<TInterface> Bind<TInterface>(this Connection connection)
        {
            if (AllBinders.ToArray().OfType<IRemoteBinder<TInterface>>().Any(x => x.Connection == connection))
                throw new NotSupportedException("Only one remote binder is permitted.");

            return new RemoteBinder<TInterface>(connection);
        }

        /// <summary>
        /// Creates two way RPC sending binding for the provided connection.
        /// <para>Shorthand for binding local and remote binder separately.</para>
        /// </summary>
        /// <typeparam name="TInterface">Interface type.</typeparam>
        /// <param name="connection">Existing connection to bind to.</param>
        /// <returns>Loca and remote binder.</returns>
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
        /// Gets all remote binders which connection also have local binder(s) associated with the specified object.
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
        /// <returns>The RPC task.</returns>
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
        /// <returns>The collection of results.</returns>
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
        /// <returns>The RPC task.</returns>
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
        /// <returns>The collection of results.</returns>
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

        public static int ConnectionCount
        {
            get
            {
                return AllBinders.ToArray()
                                 .Select(x => x.Connection)
                                 .Distinct()
                                 .Count();
            }
        }

        #endregion
    }
}
