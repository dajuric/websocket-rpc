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
using System.Net;
using System.Net.WebSockets;
using System.Threading;
using System.Threading.Tasks;

namespace WebSocketRPC
{
    /// <summary>
    /// Websocket server.
    /// </summary>
    public static class Server
    {
        /// <summary>
        /// Creates and starts a new instance of the http / websocket server.
        /// </summary>
        /// <param name="httpListenerPrefix">The http/https URI listening prefix.</param>
        /// <param name="token">Cancellation token.</param>
        /// <param name="onConnect">Action executed when connection is created.</param>
        /// <returns>Server task.</returns>
        public static async Task ListenAsync(string httpListenerPrefix, CancellationToken token, Action<Connection, WebSocketContext> onConnect)
        {
            var listener = new HttpListener();
            listener.Prefixes.Add(httpListenerPrefix);
            listener.Start();
           
            while (true)
            {
                HttpListenerContext listenerContext = await listener.GetContextAsync();

                if (listenerContext.Request.IsWebSocketRequest)
                {
                    listenAsync(listenerContext, token, onConnect).Wait(0);
                }
                else
                {
                    listenerContext.Response.StatusCode = 400;
                    listenerContext.Response.Close();
                }

                if (token.IsCancellationRequested)
                    break;
            }
            
            listener.Stop();
        }

        static async Task listenAsync(HttpListenerContext listenerContext, CancellationToken token, Action<Connection, WebSocketContext> onConnect)
        {
            if (!listenerContext.Request.IsWebSocketRequest)
                return;

            WebSocketContext ctx = null;
            WebSocket webSocket = null;
            try
            {
                ctx = await listenerContext.AcceptWebSocketAsync(subProtocol: null);
                webSocket = ctx.WebSocket;
            }
            catch (Exception)
            {
                listenerContext.Response.StatusCode = 500;
                listenerContext.Response.Close();
                return;
            }

            var connection = new Connection(webSocket, CookieUtils.GetCookies(ctx.CookieCollection));
            try
            {
                onConnect(connection, ctx);
                await Connection.ListenReceiveAsync(connection, token);
            }
            finally
            {
                webSocket?.Dispose();
            }
        }
    }
}
