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
using System.Linq;
using System.Net.WebSockets;
using System.Threading;

namespace WebsocketRPC
{
    class RelayBinder<TInterface>: BinderBase, IRelayBinder<TInterface>
    {
        IRemoteBinder<TInterface> remoteBinder = null;

        public RelayBinder(Connection connection, Func<IEnumerable<IRemoteBinder<TInterface>>> getRemoteBinders)
            : base(connection)
        {
            Connection.OnOpen += async () =>
            {
                var rBinders = getRemoteBinders();
                if (rBinders.Skip(1).Any()) //>1
                {
                    var msg = "There are multiple target connections for a single relay connection.";
                    Connection.InvokeErrorAsync(new Exception(msg));
                    await Connection.CloseAsync(WebSocketCloseStatus.PolicyViolation, msg);
                    return;
                }
                else if (rBinders.Any() == false)
                {
                    var msg = "There are no target connections for a single relay connection.";
                    Connection.InvokeErrorAsync(new Exception(msg));
                    await Connection.CloseAsync(WebSocketCloseStatus.PolicyViolation, msg);
                    return;
                }

                remoteBinder = rBinders.First();
                remoteBinder.Connection.OnReceive += onReveiveResponse; //rely to the origin
            };

            Connection.OnReceive += async (d, isText) =>
            {
                await remoteBinder.Connection.SendAsync(d);
            };

            Connection.OnClose += () =>
            {
                remoteBinder.Connection.OnReceive -= onReveiveResponse;
            };
        }

        async void onReveiveResponse(ArraySegment<byte> d, bool isText)
        {
            var msgType = isText ? WebSocketMessageType.Text : WebSocketMessageType.Binary;
            await Connection.Socket.SendAsync(d, msgType, true, CancellationToken.None);
        }
    }
}
