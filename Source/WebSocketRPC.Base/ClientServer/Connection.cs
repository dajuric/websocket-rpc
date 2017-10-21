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
using System.Diagnostics;
using System.Net;
using System.Net.WebSockets;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace WebSocketRPC
{
    /// <summary>
    /// Represents the websocket connection.
    /// </summary>
    public class Connection
    {
        internal static int MaxMessageSize { get; set; } =  64 * 1024; //x KiB
        static string messageToBig = "The message exceeds the maximum allowed message size: {0} bytes.";

        /// <summary>
        /// Gets or sets the underlying socket.
        /// </summary>
        internal WebSocket Socket { get; set; }
        /// <summary>
        /// Gets the cookie collection.
        /// </summary>
        public CookieCollection Cookies { get; internal set;  }

        /// <summary>
        /// Message receive event. Args: message, is text message.
        /// </summary>
        public event Action<ArraySegment<byte>, bool> OnReceive;
        /// <summary>
        /// Open event.
        /// </summary>
        public event Action OnOpen;
        /// <summary>
        /// Close event.
        /// </summary>
        public event Action OnClose;
        /// <summary>
        /// Error event Args: exception.
        /// </summary>
        public event Action<Exception> OnError;

        /// <summary>
        /// Sends the specified data.
        /// </summary>
        /// <param name="data">Binary data to send.</param>
        /// <returns>True if the operation was successful, false otherwise.</returns>
        public async Task<bool> SendAsync(ArraySegment<byte> data)
        {
            if (Socket.State != WebSocketState.Open)
                return false;

            if (data.Count >= MaxMessageSize)
            {
                await CloseAsync(WebSocketCloseStatus.MessageTooBig, String.Format(messageToBig, MaxMessageSize));
                return false;
            }

            await Socket.SendAsync(data, WebSocketMessageType.Binary, true, CancellationToken.None);
            return true;
        }

        /// <summary>
        /// Sends the specified data.
        /// </summary>
        /// <param name="data">Text data to send.</param>
        /// <param name="e">String encoding.</param>
        /// <returns>True if the operation was successful, false otherwise.</returns>
        public async Task<bool> SendAsync(string data, Encoding e)
        {
            if (Socket.State != WebSocketState.Open)
                return false;

            var bData = e.GetBytes(data);
            var segment = new ArraySegment<byte>(bData, 0, bData.Length);
            if (segment.Count >= MaxMessageSize)
            {
                await CloseAsync(WebSocketCloseStatus.MessageTooBig, String.Format(messageToBig, MaxMessageSize));
                return false;
            }

            Debug.WriteLine("Sending: " + data);
            await Socket.SendAsync(segment, WebSocketMessageType.Text, true, CancellationToken.None);
            return true;
        }

        /// <summary>
        /// Closes the connection.
        /// </summary>
        /// <param name="closeStatus">Close reason.</param>
        /// <param name="statusDescription">Status description.</param>
        /// <returns>Task.</returns>
        public async Task CloseAsync(WebSocketCloseStatus closeStatus = WebSocketCloseStatus.NormalClosure, string statusDescription = "")
        {
            if (Socket.State != WebSocketState.Open)
                return;

            await Socket.CloseOutputAsync(closeStatus, statusDescription, CancellationToken.None);
            OnClose?.Invoke();
            clearEvents();
        }

        /// <summary>
        /// Listens for the receive messages for the specified connection.
        /// </summary>
        /// <param name="connection">Connection.</param>
        /// <param name="token">Cancellation token.</param>
        /// <returns>Task.</returns>
        internal static async Task ListenReceiveAsync(Connection connection, CancellationToken token)
        {
            var webSocket = connection.Socket;
            using (var registration = token.Register(() => connection.CloseAsync().Wait()))
            {
                try
                {
                    connection.OnOpen?.Invoke();
                    byte[] receiveBuffer = new byte[RPCSettings.MaxMessageSize];

                    while (webSocket.State == WebSocketState.Open)
                    {
                        WebSocketReceiveResult receiveResult = null;
                        var count = 0;
                        do
                        {
                            receiveResult = await webSocket.ReceiveAsync(new ArraySegment<byte>(receiveBuffer), CancellationToken.None);
                            count += receiveResult.Count;

                            if (count >= MaxMessageSize)
                            {
                                await connection.CloseAsync(WebSocketCloseStatus.MessageTooBig, String.Format(messageToBig, MaxMessageSize));
                                return;
                            }
                        }
                        while (receiveResult?.EndOfMessage == false);


                        if (receiveResult.MessageType == WebSocketMessageType.Close)
                        {
                            await connection.CloseAsync();
                        }
                        else
                        {
                            Debug.WriteLine("Received: " + new ArraySegment<byte>(receiveBuffer, 0, count).ToString(Encoding.ASCII));
                            connection.OnReceive?.Invoke(new ArraySegment<byte>(receiveBuffer, 0, count), receiveResult.MessageType == WebSocketMessageType.Text);
                        }

                        if (token.IsCancellationRequested)
                            break;
                    }
                }
                catch (Exception ex)
                {
                    while (ex.InnerException != null) ex = ex.InnerException;
                    connection.OnError?.Invoke(ex);
                    connection.OnClose?.Invoke();
                    //socket will be aborted -> no need to close manually
                }
            }
        }

        /// <summary>
        /// Invokes the error event.
        /// </summary>
        /// <param name="ex">Exception.</param>
        internal void InvokeErrorAsync(Exception ex)
        {
            OnError?.Invoke(ex);
        }

        private void clearEvents()
        {
            OnOpen = null;
            OnClose = null;
            OnError = null;
            OnReceive = null;
        }
    }
}
