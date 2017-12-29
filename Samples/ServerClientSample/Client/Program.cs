using SampleBase;
using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using WebSocketRPC;

namespace TestClient
{
    interface ILocalAPI
    {
        Task<int> LongRunningTask(int a, int b);
    }

    class RemoteAPI //:IRemoteAPI
    {
        public void WriteProgress(float progress)
        {
            Console.Write("\rCompleted: {0}%.", progress * 100);
        }
    }

    public class Program
    {
        //if access denied execute: "netsh http delete urlacl url=http://+:8001/" (delete for 'localhost', add for public address)
        static void Main(string[] args)
        {
            //Debug.Listeners.Add(new TextWriterTraceListener(Console.Out));

            Console.ForegroundColor = ConsoleColor.Green;
            Console.WriteLine("Client\n");

            //start client and bind to its local API
            var cts = new CancellationTokenSource();
            var t = Client.ConnectAsync("ws://localhost:8001/", cts.Token, c =>
            {
                c.Bind<RemoteAPI, ILocalAPI>(new RemoteAPI());
                c.OnOpen += async () =>
                {
                    var r = await RPC.For<ILocalAPI>().CallAsync(x => x.LongRunningTask(5, 3));
                    Console.WriteLine("\nResult: " + r.First());
                };
            });

            Console.Write("{0} ", nameof(TestClient));
            AppExit.WaitFor(cts, t);
        }
    }
}
