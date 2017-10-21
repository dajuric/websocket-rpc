using Microsoft.AspNetCore.Http;
using System;
using System.Net;
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
        private Action<HttpContext, Connection> onConnect;

        /// <summary>
        /// Creates new web-socket RPC middle-ware.
        /// </summary>
        /// <param name="next">Next middle-ware in the pipeline.</param>
        /// <param name="onConnect">Action triggered when a new connection is received.</param>
        public WebSocketRPCMiddleware(RequestDelegate next,
                                      Action<HttpContext, Connection> onConnect)
        {
            this.next = next;
            this.onConnect = onConnect;
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
            var connection = new Connection { Socket = socket, Cookies = getCookies(context.Request.Cookies) };

            try
            {
                onConnect(context, connection);
                await Connection.ListenReceiveAsync(connection, CancellationToken.None);
            }
            finally
            {
                socket?.Dispose();
            }
        }

        private static CookieCollection getCookies(IRequestCookieCollection cookieCollection)
        {
            var cc = new CookieCollection();
            foreach (var k in cookieCollection.Keys)
            {
                cc.Add(new Cookie(k, cookieCollection[k]));
            }

            return cc;
        }
    }
}
