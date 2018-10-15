using Microsoft.AspNetCore.Http;
using System;
using System.Collections.Generic;
using System.Net;
using System.Threading;
using System.Threading.Tasks;

namespace WebSocketRPC
{
    /// <summary>
    /// WebSocket RPC middle-ware class.
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
        /// Invokes RPC listening task for the underlying WebSocket.
        /// </summary>
        /// <param name="context">HTTP context.</param>
        /// <returns>Listening task.</returns>
        public async Task Invoke(HttpContext context)
        {
            if (!context.WebSockets.IsWebSocketRequest)
            {
                await next(context);
                return;
            }

            var socket = await context.WebSockets.AcceptWebSocketAsync();
            var connection = new Connection(socket, getCookies(context.Request.Cookies));

            try
            {
                onConnect(context, connection);
                await connection.ListenReceiveAsync(CancellationToken.None);
            }
            finally
            {
                socket?.Dispose();
            }
        }

        private static Dictionary<string, string> getCookies(IRequestCookieCollection cookieCollection)
        {
            var cc = new Dictionary<string, string>();
            foreach (var k in cookieCollection.Keys)
            {
                if (cc.ContainsKey(k))
                    continue; //take only the first one 

                cc.Add(k, cookieCollection[k]);
            }

            return cc;
        }
    }
}
