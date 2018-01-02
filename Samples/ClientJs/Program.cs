using SampleBase;
using System;
using System.Diagnostics;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using WebSocketRPC;

namespace ClientJs
{
    /// <summary>
    /// Progress API.
    /// </summary>
    interface IProgressAPI
    {
        /// <summary>
        /// Writes progress.
        /// </summary>
        /// <param name="progress">Progress value [0..1].</param>
        void WriteProgress(float progress);
    }

    /// <summary>
    /// Task API.
    /// </summary>
    class TaskAPI
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
                await RPC.For<IProgressAPI>(this).CallAsync(x => x.WriteProgress((float)p / 100));
            }

            return a + b;
        }
    }

    class Program
    {
        //if access denied execute: "netsh http delete urlacl url=http://+:8001/" (delete for 'localhost', add for public address)
        //open Index.html to run the client
        static void Main(string[] args)
        {
            //generate js code
            File.WriteAllText($"./Site/{nameof(TaskAPI)}.js", RPCJs.GenerateCallerWithDoc<TaskAPI>());
          
            //start server and bind its local and remote API
            var cts = new CancellationTokenSource();
            var t = Server.ListenAsync("http://localhost:8001/", cts.Token, (c, ws) =>
            {
                c.Bind<TaskAPI, IProgressAPI>(new TaskAPI());
                c.BindTimeout(TimeSpan.FromSeconds(1)); //close connection if there is no incommming message after X seconds
            });

            Console.Write("{0} ", nameof(ClientJs));
            Process.Start(new ProcessStartInfo(Path.GetFullPath("./Site/Index.html")) { UseShellExecute= true });
            AppExit.WaitFor(cts, t);
        }
    }
}
