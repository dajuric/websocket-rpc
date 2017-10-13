using System;
using System.Linq;
using System.Threading;
using WebsocketRPC;

namespace TestClient
{
    public interface ILocalAPI
    {
        int LongRunningTask(int a, int b);
    }

    public class RemoteAPI //:IRemoteAPI
    {
        public bool WriteProgress(float progress)
        {
            Console.WriteLine("\rCompleted: {0}%.", progress * 100);
            return true;
        }
    }

    public class Program
    {
        //if access denied execute: "netsh http delete urlacl url=http://+:8001/"
        static void Main(string[] args)
        {
            //Debug.Listeners.Add(new TextWriterTraceListener(Console.Out));

            Console.ForegroundColor = ConsoleColor.Green;
            Console.WriteLine("Client\n");

            //start client and bind to its local API
            var cts = new CancellationTokenSource();
            Client.ConnectAsync("ws://localhost:8001/", cts.Token, c =>
            {
                c.Bind<RemoteAPI, ILocalAPI>(new RemoteAPI());
                c.OnOpen += async () =>
                {
                    var r = await RPC.For<ILocalAPI>().CallAsync(x => x.LongRunningTask(5, 3));
                    Console.WriteLine("Result: " + r.First());
                };
            })
            .Wait(0);

            Console.Write("Running: '{0}'. Press [Enter] to exit.\n", nameof(TestClient));
            Console.ReadLine();
            cts.Cancel();
        }
    }
}
