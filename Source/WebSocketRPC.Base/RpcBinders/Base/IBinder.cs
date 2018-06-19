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
    /// The base local binder.
    /// </summary>
    public interface ILocalBinder : IBinder
    {
        /// <summary>
        /// Checks whether the provided request can be processed by this IBinder.
        /// </summary>
        /// <param name="request">The request being checked.</param>
        /// <returns>True if this binder can invoke a method for this request; false otherwise.</returns>
        bool CanProcessRequest(Request request);

        /// <summary>
        /// Invoke a request.
        /// </summary>
        /// <param name="request">The invoked request.</param>
        /// <returns>The response for the given request.</returns>
        Task<Response> InvokeRequest(Request request);
    }

    /// <summary>
    /// Interface for a local binder for the specified object type.
    /// </summary>
    /// <typeparam name="TObj">Object type.</typeparam>
    public interface ILocalBinder<TObj> : ILocalBinder
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
