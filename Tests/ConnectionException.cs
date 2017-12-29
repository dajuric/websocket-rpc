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
                c.Bind<IServiceAPI>();
                c.OnOpen += () =>
                {
                    throw new NotImplementedException();
                };

                c.OnError += e =>
                {
                    Console.WriteLine("Error: " + e.Message);
                    return Task.FromResult(true);
                };
            },
            reconnectOnError: false);

            return new Task[] { ts, tc };
        }
    }
}
