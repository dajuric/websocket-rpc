using System;
using System.Linq.Expressions;
using System.Threading.Tasks;

namespace WebSocketRPC
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
        Task CallAsync(Expression<Func<T, Task>> functionExpression);

        /// <summary>
        /// Calls the RPC method.
        /// </summary>
        /// <typeparam name="TResult">Result.</typeparam>
        /// <param name="functionExpression">Method getter.</param>
        /// <returns>RPC result.</returns>
        Task<TResult> CallAsync<TResult>(Expression<Func<T, Task<TResult>>> functionExpression);
    }
}
