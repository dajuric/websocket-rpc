using Microsoft.AspNetCore.Http;
using System;
using System.Threading;
using System.Threading.Tasks;

namespace WebSocketRPC
{
    /// <summary>
    /// WebSocket RPC middle-ware.
    /// </summary>
    public class WebSocketRPCMiddleware
    {
        private readonly RequestDelegate next;
        private Action<HttpContext, Connection> onConnection;

        /// <summary>
        /// Creates new web-socket RPC middle-ware.
        /// </summary>
        /// <param name="next">Next middle-ware in the pipeline.</param>
        /// <param name="onConnection">Action triggered when a new connection is received.</param>
        public WebSocketRPCMiddleware(RequestDelegate next,
                                      Action<HttpContext, Connection> onConnection)
        {
            this.next = next;
            this.onConnection = onConnection;
        }

        /// <summary>
        /// Invokes the web-socket listener.
        /// </summary>
        /// <param name="context">HTTP context.</param>
        /// <returns>Listener task.</returns>
        public async Task Invoke(HttpContext context)
        {
            if (!context.WebSockets.IsWebSocketRequest)
            {
                await next(context);
                return;
            }

            var socket = await context.WebSockets.AcceptWebSocketAsync();

            var connection = new Connection { Socket = socket, Cookies = null /*context.Request.Cookies*/ };
            try
            {
                onConnection(context, connection);
                await Connection.ListenReceiveAsync(connection, CancellationToken.None);
            }
            finally
            {
                socket?.Dispose();
            }
        }
    }
}
