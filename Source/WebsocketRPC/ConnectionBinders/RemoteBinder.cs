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
using System.Linq.Expressions;
using System.Threading.Tasks;
using static WebsocketRPC.RPCSettings;

namespace WebsocketRPC
{
    class RemoteBinder<TInterface> : BinderBase, IRemoteBinder<TInterface>
    {
        RemoteInvoker<TInterface> rInvoker = null;

        public RemoteBinder(Connection connection)
            : base(connection)
        {
            rInvoker = new RemoteInvoker<TInterface>();

            Connection.OnOpen += async () =>
            {
                rInvoker.Initialize(r => Connection.SendAsync(r.ToJson(), Encoding));
                await Task.FromResult(0);
            };

            Connection.OnReceive += async (d, isText) =>
            {
                if (isText)
                    return;

                var msg = Response.FromJson(d.ToString(Encoding));
                if (msg.IsEmpty) return;

                rInvoker.Receive(msg);
                await Task.FromResult(0);
            };
        }

        public async Task CallAsync(Expression<Action<TInterface>> functionExpression)
        {
            await rInvoker.InvokeAsync(functionExpression);
        }

        public async Task<TResult> CallAsync<TResult>(Expression<Func<TInterface, TResult>> functionExpression)
        {
            return await rInvoker.InvokeAsync(functionExpression);
        }

        public async Task<Task> CallAsync(Expression<Func<TInterface, Task>> functionExpression)
        {
            return await rInvoker.InvokeAsync(functionExpression);
        }

        public async Task<Task<TResult>> CallAsync<TResult>(Expression<Func<TInterface, Task<TResult>>> functionExpression)
        {
            return await rInvoker.InvokeAsync(functionExpression);
        }
    }
}
