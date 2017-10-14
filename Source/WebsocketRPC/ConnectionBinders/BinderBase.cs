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
using System.Diagnostics;
using System.Linq.Expressions;
using System.Threading.Tasks;

namespace WebsocketRPC
{
    /// <summary>
    /// The base binder interface.
    /// </summary>
    public interface IBinder
    {
        /// <summary>
        /// Gets the associated connection.
        /// </summary>
        Connection Connection { get; }
    }

    /// <summary>
    /// Local binder interface for the specified object type.
    /// </summary>
    /// <typeparam name="T">Object type.</typeparam>
    public interface ILocalBinder<T> : IBinder
    {
        /// <summary>
        /// Gets the associated object.
        /// </summary>
        T Object { get; }
    }

    /// <summary>
    /// Remote binder interface for the specified interface type.
    /// </summary>
    /// <typeparam name="T">Interface type.</typeparam>
    public interface IRemoteBinder<T> : IBinder
    {
        /// <summary>
        /// Calls the RPC method.
        /// </summary>
        /// <param name="functionExpression">Method getter.</param>
        /// <returns>RPC invoking task.</returns>
        Task CallAsync(Expression<Action<T>> functionExpression);

        /// <summary>
        /// Calls the RPC method.
        /// </summary>
        /// <typeparam name="TResult">Result.</typeparam>
        /// <param name="functionExpression">Method getter.</param>
        /// <returns>RPC result.</returns>
        Task<TResult> CallAsync<TResult>(Expression<Func<T, TResult>> functionExpression);


        /// <summary>
        /// Calls the RPC method.
        /// </summary>
        /// <param name="functionExpression">Method getter.</param>
        /// <returns>RPC task result.</returns>
        Task<Task> CallAsync(Expression<Func<T, Task>> functionExpression);

        /// <summary>
        /// Calls the RPC method.
        /// </summary>
        /// <typeparam name="TResult">Result.</typeparam>
        /// <param name="functionExpression">Method getter.</param>
        /// <returns>RPC result.</returns>
        Task<Task<TResult>> CallAsync<TResult>(Expression<Func<T, Task<TResult>>> functionExpression);
    }

    /// <summary>
    /// Relay connection binder for the specified interface type.
    /// </summary>
    /// <typeparam name="T">Interface type.</typeparam>
    public interface IRelayBinder<T> : IBinder
    { }

    abstract class BinderBase : IBinder
    {
        public Connection Connection { get; private set; }

        protected BinderBase(Connection connection)
        {
            Connection = connection;

            Connection.OnOpen += () =>
            {
                Debug.WriteLine("Open");

                RPC.AllBinders.Add(this);
            };

            Connection.OnClose += () =>
            {
                Debug.WriteLine("Close");

                RPC.AllBinders.Remove(this);
            };

            Connection.OnError += e =>
            {
                Debug.WriteLine("Error");

                RPC.AllBinders.Remove(this);
            };
        }
    }
}
