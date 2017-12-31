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



        class ServiceTimeoutAPI : IServiceTimeoutAPI
        {
            public async Task<int> LongRunningTask(int a, int b)
            {
                await Task.Delay(TimeSpan.FromSeconds(5));
                return a + b;
            }
        }

        interface IServiceTimeoutAPI
        {
            Task<int> LongRunningTask(int a, int b);
        }

        static Task[] TestRpcTimeout(CancellationTokenSource cts)
        {
            var ts = Server.ListenAsync($"http://{address}", cts.Token, (c, wc) => c.Bind(new ServiceTimeoutAPI()));
            var tc = Client.ConnectAsync($"ws://{address}", cts.Token, c =>
            {
                c.Bind<IServiceTimeoutAPI>(); //should generate an exception: must be an interface
                c.OnOpen += async () =>
                {
                    try
                    {
                        var results = await RPC.For<IServiceTimeoutAPI>().CallAsync(x => x.LongRunningTask(1, 2));
                    }
                    catch (Exception ex)
                    {
                        Console.WriteLine("Error while executing {0}: {1}", nameof(ServiceTimeoutAPI.LongRunningTask), ex.Message);
                    }
                };

                c.OnError += e => Task.Run(() => Console.WriteLine("Error: " + e.Message));
            },
            reconnectOnError: false);

            return new Task[] { ts, tc };
        }
    }
}
