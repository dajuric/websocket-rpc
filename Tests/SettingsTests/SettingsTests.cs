using System;
using System.Threading;
using System.Threading.Tasks;
using WebSocketRPC;

namespace Tests
{
    partial class Program
    {
        static Task[] TestMaxMessageSize(CancellationTokenSource cts)
        {
            Connection.MaxMessageSize = Connection.Encoding.GetMaxByteCount(10);

            var ts = Server.ListenAsync($"http://{address}", cts.Token, (c, wc) => c.Bind(new ServiceAPI()));
            var tc = Client.ConnectAsync($"ws://{address}", cts.Token, c =>
            {
                c.OnOpen += async () =>
                {
                    await c.SendAsync("Long long message.");
                };

                c.OnError += e => Task.Run(() => Console.WriteLine("Error: " + e.Message));
                c.OnClose += (s, d) => Task.Run(() => Console.WriteLine("Close: " + d));
            },
            reconnectOnError: false);

            return new Task[] { ts, tc };
        }
    }
}
