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
using System.Net.WebSockets;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Linq;

namespace WebSocketRPC
{
    /// <summary>
    /// Represents the websocket connection.
    /// </summary>
    public class Connection
    {
        #region Global settings

        static int maxMessageSize = 64 * 1024; //x KiB
        /// <summary>
        /// Gets or sets the maximum message size in bytes [1..Int32.MaxValue].
        /// </summary>
        public static int MaxMessageSize
        {
            get { return maxMessageSize; }
            set
            {
                if (value <= 0)
                    throw new ArgumentOutOfRangeException("The message size must be set to a strictly positive value.");

                maxMessageSize = value;
            }
        }

        static Encoding encoding = Encoding.ASCII;
        /// <summary>
        /// Gets or sets the string RPC messaging encoding.
        /// </summary>
        public static Encoding Encoding
        {
            get { return encoding; }
            set
            {
                if (encoding == null)
                    throw new ArgumentException("The provided value must not be null.");

                encoding = value;
            }
        }

        static string messageToBig = "The message exceeds the maximum allowed message size: {0} of allowed {1} bytes.";

        #endregion

        WebSocket socket;
        TaskQueue sendTaskQueue;

        /// <summary>
        /// Creates new connection.
        /// </summary>
        /// <param name="socket">Web-socket.</param>
        /// <param name="cookies">Cookies.</param>
        internal protected Connection(WebSocket socket, IReadOnlyDictionary<string, string> cookies)
        {
            this.socket = socket;
            this.sendTaskQueue = new TaskQueue();
            this.Cookies = cookies;
        }

        /// <summary>
        /// Gets the cookie collection.
        /// </summary>
        public IReadOnlyDictionary<string, string> Cookies { get; private set; }

        /// <summary>
        /// Gets whether the connection is opened or not.
        /// </summary>
        public bool IsAlive => socket?.State == WebSocketState.Open;

        #region Events

        /// <summary>
        /// Message receive event. Message is decoded using <seealso cref="Encoding"/>.
        /// </summary>
        public event Func<string, Task> OnReceive;
        /// <summary>
        /// Open event.
        /// </summary>
        public event Func<Task> OnOpen;
        /// <summary>
        /// Close event.
        /// </summary>
        public event Func<WebSocketCloseStatus, string, Task> OnClose;
        /// <summary>
        /// Error event Args: exception.
        /// </summary>
        public event Func<Exception, Task> OnError;

        /// <summary>
        /// Invokes the error event.
        /// </summary>
        /// <param name="exception">Exception.</param>
        internal void InvokeOnError(Exception exception)
        {
            if (OnError == null || exception == null)
                return;

            try
            {
                var members = OnError.GetInvocationList().Cast<Func<Exception, Task>>();

                Task.WhenAll(members.Select(x => x(exception)));
                    //.Wait(0);
            }
            catch (Exception ex) when (ex.InnerException is TaskCanceledException)
            { }
        }

        private void invokeOnOpen()
        {
            if (OnOpen == null)
                return;

            try
            {
                var members = OnOpen.GetInvocationList().Cast<Func<Task>>();

                Task.WhenAll(members.Select(x => x()))
                    .ContinueWith(t => InvokeOnError(t.Exception), TaskContinuationOptions.OnlyOnFaulted);
                    //.Wait(0);
            }
            catch (Exception ex) when (ex.InnerException is TaskCanceledException)
            { }
        }

        private void invokeOnReceive(string msg)
        {
            if (OnReceive == null)
                return;

            try
            {
                var members = OnReceive.GetInvocationList().Cast<Func<string, Task>>();

                Task.WhenAll(members.Select(x => x(msg)))
                    .ContinueWith(t => InvokeOnError(t.Exception), TaskContinuationOptions.OnlyOnFaulted);
                    //.Wait(0); //throws TaskCanceledException -> why ?
            }
            catch (Exception ex) when (ex.InnerException is TaskCanceledException)
            { }
        }

        private void invokeOnClose(WebSocketCloseStatus closeStatus, string statusDescription)
        {
            if (OnClose == null)
                return;

            try
            {
                var members = OnClose.GetInvocationList().Cast<Func< WebSocketCloseStatus, string, Task >>();

                Task.WhenAll(members.Select(x => x(closeStatus, statusDescription)))
                    .ContinueWith(t => InvokeOnError(t.Exception), TaskContinuationOptions.OnlyOnFaulted);
                    //.Wait(0);
            }
            catch (Exception ex) when (ex.InnerException is TaskCanceledException)
            { }
        }

        #endregion

        #region Send

        /// <summary>
        /// Sends the specified data as the text message type.
        /// </summary>
        /// <param name="data">Text data to send.</param>
        /// <returns>True if the operation was successful, false otherwise.</returns>
        public async Task<bool> SendAsync(string data)
        {
            if (data == null)
                throw new ArgumentNullException(nameof(data), "The provided tet must not be null.");

            if (socket.State != WebSocketState.Open)
                return false;

            var bData = Encoding.GetBytes(data);
            if (bData.Length >= MaxMessageSize)
            {
                //InvokeOnError(new NotSupportedException(String.Format(messageToBig, maxMessageSize)));
                await CloseAsync(WebSocketCloseStatus.MessageTooBig, String.Format(messageToBig, bData.Length, maxMessageSize));
                return false;
            }

            Debug.WriteLine("Sending: " + data);
            var segment = new ArraySegment<byte>(bData, 0, bData.Length);
            await sendTaskQueue.Enqueue(() => sendAsync(segment, WebSocketMessageType.Text));
            return true;
        }

        async Task sendAsync(ArraySegment<byte> data, WebSocketMessageType msgType)
        {
            if (socket.State != WebSocketState.Open)
                return;

            try
            {
                await socket.SendAsync(data, msgType, true, CancellationToken.None);
            }
            catch (Exception ex)
            {
                if (socket.State != WebSocketState.Open)
                    await CloseAsync(WebSocketCloseStatus.InternalServerError, ex.Message);
            }
        }

        #endregion

        #region Close

        /// <summary>
        /// Closes the connection.
        /// </summary>
        /// <param name="closeStatus">Close reason.</param>
        /// <param name="statusDescription">Status description.</param>
        /// <returns>Task.</returns>
        public async Task CloseAsync(WebSocketCloseStatus closeStatus = WebSocketCloseStatus.NormalClosure, string statusDescription = "")
        {
            try
            {
                if (socket.State == WebSocketState.Open || socket.State == WebSocketState.CloseReceived)
                    await socket.CloseOutputAsync(closeStatus, statusDescription, CancellationToken.None);
            }
            catch(Exception ex)
            {
                InvokeOnError(ex);
            }
            finally
            {
                invokeOnClose(closeStatus, statusDescription);
                clearEvents();
            }
        }

        private void clearEvents()
        {
            OnClose = null;
            OnError = null;
            OnReceive = null;
        }

        #endregion

        #region Receive (listen)

        /// <summary>
        /// Listens for the receive messages for the specified connection.
        /// </summary>
        /// <param name="token">Cancellation token.</param>
        /// <returns>Task.</returns>
        internal async Task ListenReceiveAsync(CancellationToken token)
        {
            using (var registration = token.Register(() => CloseAsync().Wait()))
            {
                try
                {
                    await listenReceiveAsync(token);
                }
                catch (Exception ex)
                {
                    InvokeOnError(ex);
                    await CloseAsync(WebSocketCloseStatus.InternalServerError, ex.Message);
                    //socket will be aborted -> no need to close manually
                }
            }
        }

        async Task listenReceiveAsync(CancellationToken token)
        {
            invokeOnOpen();
            byte[] receiveBuffer = new byte[maxMessageSize];

            while (socket.State == WebSocketState.Open)
            {
                //receive
                WebSocketReceiveResult receiveResult = null;
                var count = 0;
                do
                {
                    var segment = new ArraySegment<byte>(receiveBuffer, count, maxMessageSize - count);
                    receiveResult = await socket.ReceiveAsync(segment, CancellationToken.None);
                    count += receiveResult.Count;

                    if (count >= maxMessageSize)
                    {
                        //InvokeOnError(new NotSupportedException(String.Format(messageToBig, maxMessageSize)));
                        await CloseAsync(WebSocketCloseStatus.MessageTooBig, String.Format(messageToBig, count, maxMessageSize));
                        return;
                    }
                }
                while (receiveResult?.EndOfMessage == false);

                //process response
                switch (receiveResult.MessageType)
                {
                    case WebSocketMessageType.Close:
                        await CloseAsync();
                        break;
                    case WebSocketMessageType.Binary:
                        InvokeOnError(new NotSupportedException($"Binary messages are not supported. Received {count} bytes."));
                        Debug.WriteLine("Received binary.");
                        break;
                    default:
                        var segment = new ArraySegment<byte>(receiveBuffer, 0, count);
                        var msg = segment.ToString(encoding);

                        invokeOnReceive(msg);
                        Debug.WriteLine("Received: " + msg);
                        break;
                }

                //check if cancellation is requested
                if (token.IsCancellationRequested)
                    break;
            }
        }

        #endregion
    }
}

