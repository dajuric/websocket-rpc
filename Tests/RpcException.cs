using System;
using System.Threading;
using System.Threading.Tasks;
using WebSocketRPC;

namespace Tests
{
    partial class Program
    {
        interface IServiceAPI
        {
            int LongRunningTask(int a, int b);
        }

        class ServiceAPI : IServiceAPI
        {
            public int LongRunningTask(int a, int b)
            {
                throw new NotImplementedException("The method is not implemented.");
            }
        }

        static Task[] TestRpcInitializeException(CancellationTokenSource cts)
        {
            var ts = Server.ListenAsync($"http://{address}", cts.Token, (c, wc) => c.Bind(new ServiceAPI()));
            var tc = Client.ConnectAsync($"ws://{address}", cts.Token, c =>
            {
                c.Bind<ServiceAPI>();
                c.OnOpen += async () =>
                {
                    try
                    {
                        var results = await RPC.For<ServiceAPI>().CallAsync(x => x.LongRunningTask(1, 2));
                    }
                    catch (Exception ex)
                    {
                        Console.WriteLine("Error while executing {0}: {1}", nameof(ServiceAPI.LongRunningTask), ex.Message);
                    }
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

        static Task[] TestRpcUnhandledException(CancellationTokenSource cts)
        {
            var ts = Server.ListenAsync($"http://{address}", cts.Token, (c, wc) => c.Bind(new ServiceAPI()));
            var tc = Client.ConnectAsync($"ws://{address}", cts.Token, c =>
            {
                c.Bind<IServiceAPI>();
                c.OnOpen += async () =>
                {
                    var results = await RPC.For<IServiceAPI>().CallAsync(x => x.LongRunningTask(1, 2));
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

        static Task[] TestRpcHandledException(CancellationTokenSource cts)
        {
            var ts = Server.ListenAsync($"http://{address}", cts.Token, (c, wc) => c.Bind(new ServiceAPI()));
            var tc = Client.ConnectAsync($"ws://{address}", cts.Token, c =>
            {
                c.Bind<IServiceAPI>();
                c.OnOpen += async () =>
                {
                    try
                    {
                        var results = await RPC.For<IServiceAPI>().CallAsync(x => x.LongRunningTask(1, 2));
                    }
                    catch (Exception ex)
                    {
                        Console.WriteLine("Error while executing {0}: {1}", nameof(ServiceAPI.LongRunningTask), ex.Message);
                    }
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
