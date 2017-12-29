using SampleBase;
using System;
using System.Threading;
using System.Threading.Tasks;
using WebSocketRPC;

namespace TestServer
{
    interface IRemoteAPI
    {
        void WriteProgress(float progress);
    }

    class LocalAPI
    {
        public async Task<int> LongRunningTask(int a, int b)
        {
            for (var p = 0; p <= 100; p += 5)
            {
                await Task.Delay(250);
                await RPC.For<IRemoteAPI>(this).CallAsync(x => x.WriteProgress((float)p / 100));
            }
            
            return a + b;
        }
    }

    public class Program
    {
        //if access denied execute: "netsh http delete urlacl url=http://+:8001/" (delete for 'ocalhost', add for public address)
        static void Main(string[] args)
        {
            Console.ForegroundColor = ConsoleColor.Red;
            Console.WriteLine("Server\n");

            //start server and bind to its local and remote API
            var cts = new CancellationTokenSource();
            var t = Server.ListenAsync("http://localhost:8001/", cts.Token, (c, wc) => c.Bind<LocalAPI, IRemoteAPI>(new LocalAPI()));

            Console.Write("{0} ", nameof(TestServer));
            AppExit.WaitFor(cts, t);
        }
    }
}
