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
using System.Net.WebSockets;
using System.Threading;
using System.Threading.Tasks;

namespace WebsocketRPC
{
    /// <summary>
    /// Websocket client.
    /// </summary>
    public static class Client
    {
        /// <summary>
        /// Creates and starts a new websocket listening client.
        /// </summary>
        /// <param name="uri">The target uri of the format: "ws://(address)/[path]".</param>
        /// <param name="token">Cancellation token.</param>
        /// <param name="onConnection">Action executed when connection is established.</param>
        /// <param name="setOptions">Websocket option set method.</param>
        /// <returns>Client task.</returns>
        public static async Task ConnectAsync(string uri, CancellationToken token, Action<Connection> onConnection, Action<ClientWebSocketOptions> setOptions = null)
        {
            ClientWebSocket webSocket = null;

            try
            {
                webSocket = new ClientWebSocket();
                setOptions?.Invoke(webSocket.Options);
                await webSocket.ConnectAsync(new Uri(uri), token);
            }
            catch (Exception ex)
            {
                webSocket?.Dispose();
                throw ex;
            }

            var connection = new Connection { Socket = webSocket, Cookies = webSocket.Options.Cookies?.GetCookies(new Uri(uri)) };
            try
            {
                onConnection(connection);
                connection.InvokeOpenAsync();
                await Connection.ListenReceiveAsync(connection, token);
            }
            finally
            {
                webSocket?.Dispose();
            }
        }
    }
}
