using System;
using System.Threading;
using System.Threading.Tasks;
using WebSocketRPC;

namespace Tests
{
    partial class Program
    {
        static Task[] TestConnectionTimeout(CancellationTokenSource cts)
        {
            var ts = Server.ListenAsync($"http://{address}", cts.Token, (c, wc) => c.Bind(new ServiceAPI()));
            var tc = Client.ConnectAsync($"ws://{address}", cts.Token, c =>
            {
                c.BindTimeout(TimeSpan.FromSeconds(5));

                c.OnError += e => Task.Run(() => Console.WriteLine("Error: " + e.Message));
                c.OnClose += (s, d) => Task.Run(() => Console.WriteLine("Close: " + d));
            },
            reconnectOnError: false);

            return new Task[] { ts, tc };
        }
    }
}
