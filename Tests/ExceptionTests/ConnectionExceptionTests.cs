using System;
using System.Threading;
using System.Threading.Tasks;
using WebSocketRPC;

namespace Tests
{
    partial class Program
    {
        static Task[] TestConnectionUnhandledException(CancellationTokenSource cts)
        {
            var ts = Server.ListenAsync($"http://{address}", cts.Token, (c, wc) => c.Bind(new ServiceAPI()));
            var tc = Client.ConnectAsync($"ws://{address}", cts.Token, c =>
            {
                c.OnOpen += () =>
                {
                    throw new NotImplementedException();
                };

                c.OnError += e => Task.Run(() => Console.WriteLine("Error: " + e.Message));
            },
            reconnectOnError: false);

            return new Task[] { ts, tc };
        }
    }
}
