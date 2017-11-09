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
        /// <param name="port">The http/https URI listening port.</param>
        /// <param name="token">Cancellation token.</param>
        /// <param name="onConnect">Action executed when connection is created.</param>
        /// <returns>Server task.</returns>
        public static async Task ListenAsync(int port, CancellationToken token, Action<Connection, WebSocketContext> onConnect)
        {
            await ListenAsync($"http://+:{port}/", token, onConnect);
        }

        /// <summary>
        /// Creates and starts a new instance of the http / websocket server.
        /// <para>All HTTP requests will have the 'BadRequest' response.</para>
        /// </summary>
        /// <param name="port">The http/https URI listening port.</param>
        /// <param name="token">Cancellation token.</param>
        /// <param name="onConnect">Action executed when connection is created.</param>
        /// <param name="onHttpRequest">Action executed on HTTP request.</param>
        /// <returns>Server task.</returns>
        public static async Task ListenAsync(int port, CancellationToken token, Action<Connection, WebSocketContext> onConnect, Action<HttpListenerRequest, HttpListenerResponse> onHttpRequest)
        {
            await ListenAsync($"http://+:{port}/", token, onConnect, onHttpRequest);
        }



        /// <summary>
        /// Creates and starts a new instance of the http / websocket server.
        /// </summary>
        /// <param name="httpListenerPrefix">The http/https URI listening prefix.</param>
        /// <param name="token">Cancellation token.</param>
        /// <param name="onConnect">Action executed when connection is created.</param>
        /// <returns>Server task.</returns>
        public static async Task ListenAsync(string httpListenerPrefix, CancellationToken token, Action<Connection, WebSocketContext> onConnect)
        {
            await ListenAsync(httpListenerPrefix, token, onConnect, (rq, rp) => 
            {
                rp.StatusCode = (int)HttpStatusCode.BadRequest;
            });
        }

        /// <summary>
        /// Creates and starts a new instance of the http / websocket server.
        /// <para>All HTTP requests will have the 'BadRequest' response.</para>
        /// </summary>
        /// <param name="httpListenerPrefix">The http/https URI listening prefix.</param>
        /// <param name="token">Cancellation token.</param>
        /// <param name="onConnect">Action executed when connection is created.</param>
        /// <param name="onHttpRequest">Action executed on HTTP request.</param>
        /// <returns>Server task.</returns>
        public static async Task ListenAsync(string httpListenerPrefix, CancellationToken token, Action<Connection, WebSocketContext> onConnect, Action<HttpListenerRequest, HttpListenerResponse> onHttpRequest)
        {
            var listener = new HttpListener();
            listener.Prefixes.Add(httpListenerPrefix);

            try { listener.Start(); }
            catch (Exception ex) when ((ex as HttpListenerException)?.ErrorCode == 5)
            {
                throw new UnauthorizedAccessException($"The HTTP server can not be started, as the namespace reservation does not exist.\n" +
                                                      $"Please run (elevated): 'netsh add urlacl url={httpListenerPrefix} user=\"Everyone\"'.");
            }

            ///using (var r = token.Register(() => listener.Stop()))
            {
                bool shouldStop = false;
                while (!shouldStop)
                {
                    try
                    {
                        HttpListenerContext ctx = await listener.GetContextAsync();

                        if (ctx.Request.IsWebSocketRequest)
                        {
                            listenAsync(ctx, token, onConnect).Wait(0);
                        }
                        else
                        {
                            onHttpRequest(ctx.Request, ctx.Response);
                            ctx.Response.Close();
                        }
                    }
                    catch (Exception)
                    {
                        if (!token.IsCancellationRequested)
                            throw;
                    }
                    finally
                    {
                        if (token.IsCancellationRequested)
                            shouldStop = true;
                    }
                }
            }
        }

        static async Task listenAsync(HttpListenerContext ctx, CancellationToken token, Action<Connection, WebSocketContext> onConnect)
        {
            if (!ctx.Request.IsWebSocketRequest)
                return;

            WebSocketContext wsCtx = null;
            WebSocket webSocket = null;
            try
            {
                wsCtx = await ctx.AcceptWebSocketAsync(subProtocol: null);
                webSocket = wsCtx.WebSocket;
            }
            catch (Exception)
            {
                ctx.Response.StatusCode = 500;
                ctx.Response.Close();
                return;
            }

            var connection = new Connection(webSocket, CookieUtils.GetCookies(wsCtx.CookieCollection));
            try
            {
                onConnect(connection, wsCtx);
                await Connection.ListenReceiveAsync(connection, token);
            }
            finally
            {
                webSocket?.Dispose();
            }
        }
    }
}
