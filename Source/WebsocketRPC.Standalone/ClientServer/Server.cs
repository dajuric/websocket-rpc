#region License
// Copyright © 2018 Darko Jurić
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
using System.Diagnostics;
using System.Linq;
using System.Net;
using System.Net.WebSockets;
using System.Text.RegularExpressions;
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
        /// Creates and starts a new instance of the http(s) / websocket server.
		/// <para>All HTTP requests will have the 'BadRequest' response by default.</para>
        /// </summary>
        /// <param name="port">The http/https URI listening port.</param>
        /// <param name="token">Cancellation token.</param>
        /// <param name="onConnect">Action executed when connection is created.</param>
        /// <param name="useHttps">True to add 'https://' prefix insteaad of 'http://'.</param>
        /// <returns>Server listening task.</returns>
        public static async Task ListenAsync(int port, CancellationToken token, Action<Connection, WebSocketContext> onConnect, bool useHttps = false)
        {
            if (port < 0 || port > UInt16.MaxValue)
                throw new NotSupportedException($"The provided port value must in the range: [0..{UInt16.MaxValue}");

            var s = useHttps ? "s" : String.Empty;
            await ListenAsync($"http{s}://+:{port}/", token, onConnect);
        }

        /// <summary>
        /// Creates and starts a new instance of the http / websocket server.
        /// <para>All HTTP requests will have the 'BadRequest' response by default.</para>
        /// </summary>
        /// <param name="port">The http/https URI listening port.</param>
        /// <param name="token">Cancellation token.</param>
        /// <param name="onConnect">Action executed when connection is created.</param>
        /// <param name="onHttpRequestAsync">Action executed on HTTP request.</param>
        /// <param name="useHttps">True to add 'https://' prefix insteaad of 'http://'.</param>
        /// <returns>Server listening task.</returns>
        public static async Task ListenAsync(int port, CancellationToken token, Action<Connection, WebSocketContext> onConnect, Func<HttpListenerRequest, HttpListenerResponse, Task> onHttpRequestAsync, bool useHttps = false)
        {
            if (port < 0 || port > UInt16.MaxValue)
                throw new NotSupportedException($"The provided port value must in the range: [0..{UInt16.MaxValue}");

            var s = useHttps ? "s" : String.Empty;
            await ListenAsync($"http{s}://+:{port}/", token, onConnect, onHttpRequestAsync);
        }



        /// <summary>
        /// Creates and starts a new instance of the http / websocket server.
		/// <para>All HTTP requests will have the 'BadRequest' response by default.</para>
        /// </summary>
        /// <param name="httpListenerPrefix">The http/https URI listening prefix.</param>
        /// <param name="token">Cancellation token.</param>
        /// <param name="onConnect">Action executed when connection is created.</param>
        /// <returns>Server listening task.</returns>
        public static async Task ListenAsync(string httpListenerPrefix, CancellationToken token, Action<Connection, WebSocketContext> onConnect)
        {
            await ListenAsync(httpListenerPrefix, token, onConnect, (rq, rp) => 
            {
                rp.StatusCode = (int)HttpStatusCode.BadRequest;
                return Task.FromResult(true);
            });
        }

        /// <summary>
        /// Creates and starts a new instance of the http / websocket server.
        /// </summary>
        /// <param name="httpListenerPrefix">The http/https URI listening prefix.</param>
        /// <param name="token">Cancellation token.</param>
        /// <param name="onConnect">Action executed when connection is created.</param>
        /// <param name="onHttpRequestAsync">Action executed on HTTP request.</param>
        /// <returns>Server listening task.</returns>
        public static async Task ListenAsync(string httpListenerPrefix, CancellationToken token, Action<Connection, WebSocketContext> onConnect, Func<HttpListenerRequest, HttpListenerResponse, Task> onHttpRequestAsync)
        {
            if (token == null)
                throw new ArgumentNullException(nameof(token), "The provided token must not be null.");

            if (onConnect == null)
                throw new ArgumentNullException(nameof(onConnect), "The provided connection action must not be null.");

            if (onHttpRequestAsync == null)
                throw new ArgumentNullException(nameof(onHttpRequestAsync), "The provided HTTP request/response action must not be null.");


            var listener = new HttpListener();
            try { listener.Prefixes.Add(httpListenerPrefix); }
            catch (Exception ex) { throw new ArgumentException("The provided prefix is not supported. Prefixes have the format: 'http(s)://+:(port)/'", ex); }

            try { listener.Start(); }
            catch (Exception ex) when ((ex as HttpListenerException)?.ErrorCode == 5)
            {
                var msg = getNamespaceReservationExceptionMessage(httpListenerPrefix);
                throw new UnauthorizedAccessException(msg, ex);
            }

			//helpful: https://stackoverflow.com/questions/11167183/multi-threaded-httplistener-with-await-async-and-tasks
			//         https://github.com/NancyFx/Nancy/blob/815b6fdf42a5a8c61e875501e305382f46cec619/src/Nancy.Hosting.Self/HostConfiguration.cs
            using (var r = token.Register(() => closeListener(listener)))
            {
                bool shouldStop = false;
                while (!shouldStop)
                {
                    try
                    {
                        var ctx = await listener.GetContextAsync();

                        if (ctx.Request.IsWebSocketRequest)
                            Task.Factory.StartNew(() => listenAsync(ctx, token, onConnect),       TaskCreationOptions.LongRunning).Wait(0);
                        else
                            Task.Factory.StartNew(() => listenHttpAsync(ctx, onHttpRequestAsync), TaskCreationOptions.None).Wait(0);
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

            Debug.WriteLine("Server stopped.");
        }

		static string getNamespaceReservationExceptionMessage(string httpListenerPrefix)
        {
            string msg = null;
            var m = Regex.Match(httpListenerPrefix, @"(?<protocol>\w+)://localhost:?(?<port>\d*)");

            if (m.Success)
            {
                var protocol = m.Groups["protocol"].Value;
                var port = m.Groups["port"].Value; if (String.IsNullOrEmpty(port)) port = 80.ToString();

                msg = $"The HTTP server can not be started, as the namespace reservation already exists.\n" +
                      $"Please run (elevated): 'netsh http delete urlacl url={protocol}://+:{port}/'.";
            }
            else
            {
                msg = $"The HTTP server can not be started, as the namespace reservation does not exist.\n" +
                      $"Please run (elevated): 'netsh http add urlacl url={httpListenerPrefix} user=\"Everyone\"'.";
            }

            return msg;
        }
		
        static void closeListener(HttpListener listener)
        {
            var wsCloseTasks = new Task[connections.Count];

            for (int i = 0; i < connections.Count; i++)
                wsCloseTasks[i] = connections[i].CloseAsync();

            Task.WaitAll(wsCloseTasks.Where(t => t != null).ToArray()); //tasks will be null if 'CloseAsync' fails
            listener.Stop();
            connections.Clear();
        }

        static async Task listenHttpAsync(HttpListenerContext ctx, Func<HttpListenerRequest, HttpListenerResponse, Task> onHttpRequest)
        {
            await onHttpRequest(ctx.Request, ctx.Response);
            ctx.Response.Close();
        }

        static List<Connection> connections = new List<Connection>();
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
                ctx.Response.StatusCode = (int)HttpStatusCode.InternalServerError;
                ctx.Response.Close();
                return;
            }

            var connection = new Connection(webSocket, CookieUtils.GetCookies(wsCtx.CookieCollection));
            try
            {
                lock (connections) connections.Add(connection);
                onConnect(connection, wsCtx);
                await connection.ListenReceiveAsync(token);
            }
            catch (Exception ex)
            {
                 connection.InvokeOnError(ex);
            }
            finally
            {
                webSocket?.Dispose();
                lock (connections) connections.Remove(connection);
            }
        }
    }
}
