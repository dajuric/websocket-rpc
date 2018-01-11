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
    /// Interface for a local binder for the specified object type.
    /// </summary>
    /// <typeparam name="TObj">Object type.</typeparam>
    public interface ILocalBinder<TObj> : IBinder
    {
        /// <summary>
        /// Gets the associated object.
        /// </summary>
        TObj Object { get; }
    }

    /// <summary>
    /// Interface for a remote binder for the specified interface type.
    /// </summary>
    /// <typeparam name="TInterface">Interface type.</typeparam>
    public interface IRemoteBinder<TInterface> : IBinder
    {
        /// <summary>
        /// Calls the RPC method.
        /// </summary>
        /// <param name="functionExpression">Method getter.</param>
        /// <returns>RPC invoking task.</returns>
        Task CallAsync(Expression<Action<TInterface>> functionExpression);

        /// <summary>
        /// Calls the RPC method.
        /// </summary>
        /// <typeparam name="TResult">Result.</typeparam>
        /// <param name="functionExpression">Method getter.</param>
        /// <returns>RPC invoking task containing the return method value.</returns>
        Task<TResult> CallAsync<TResult>(Expression<Func<TInterface, TResult>> functionExpression);


        /// <summary>
        /// Calls the RPC method.
        /// </summary>
        /// <param name="functionExpression">Method getter.</param>
        /// <returns>RPC invoking task.</returns>
        Task CallAsync(Expression<Func<TInterface, Task>> functionExpression);

        /// <summary>
        /// Calls the RPC method.
        /// </summary>
        /// <typeparam name="TResult">Result.</typeparam>
        /// <param name="functionExpression">Method getter.</param>
        /// <returns>RPC invoking task containing the return method value.</returns>
        Task<TResult> CallAsync<TResult>(Expression<Func<TInterface, Task<TResult>>> functionExpression);
    }
}
