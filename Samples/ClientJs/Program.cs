using System;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using WebsocketRPC;

namespace TestClientJs
{
    interface IRemoteAPI
    {
        bool WriteProgress(float progress);
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
        //if access denied execute: "netsh http delete urlacl url=http://+:8001/"
        static void Main(string[] args)
        {
            //generate js code
            File.WriteAllText($"../Site/{nameof(LocalAPI)}.js", RPCJs.GenerateCallerWithDoc<LocalAPI>());

            //start server and bind its local and remote API
            var cts = new CancellationTokenSource();
            Server.ListenAsync("http://localhost:8001/", cts.Token, (c, ws) => c.Bind<LocalAPI, IRemoteAPI>(new LocalAPI())).Wait(0);

            Console.Write("Running: '{0}'. Press [Enter] to exit.", nameof(TestClientJs));
            Console.ReadLine();
            cts.Cancel();
        }
    }
}
