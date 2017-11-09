using System;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using WebSocketRPC;

namespace TestClientJs
{
    /// <summary>
    /// Remote API.
    /// </summary>
    interface IRemoteAPI
    {
        /// <summary>
        /// Writes progress.
        /// </summary>
        /// <param name="progress">Progress value [0..1].</param>
        void WriteProgress(float progress);
    }

    /// <summary>
    /// Local API.
    /// </summary>
    class LocalAPI
    {
        /// <summary>
        /// Executes long running addition task.
        /// </summary>
        /// <param name="a">First number.</param>
        /// <param name="b">Second number.</param>
        /// <returns>Result.</returns>
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

    class Program
    {
        //if access denied execute: "netsh http delete urlacl url=http://+:8001/"
        //open Index.html to run the client
        static void Main(string[] args)
        {
            //generate js code
            File.WriteAllText($"./Site/{nameof(LocalAPI)}.js", RPCJs.GenerateCallerWithDoc<LocalAPI>());

            //start server and bind its local and remote API
            var cts = new CancellationTokenSource();
            var s = Server.ListenAsync("http://localhost:8001/", cts.Token, (c, ws) => c.Bind<LocalAPI, IRemoteAPI>(new LocalAPI()));

            Console.Write("Running: '{0}'. Press [Enter] to exit.", nameof(TestClientJs));
            Console.ReadLine();
            cts.Cancel();
            s.Wait();
        }
    }
}
