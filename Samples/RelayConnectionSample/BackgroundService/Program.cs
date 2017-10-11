using System;
using System.Threading;
using WebsocketRPC;

namespace BackgroundService
{
    public class BackgroundService
    {
        public string ConcatString(string a, string b)
        {
            return a + b;
        }
    }

    public class Program
    {
        //if access denied execute: "netsh http delete urlacl url=http://+:9001/"
        static void Main(string[] args)
        {
            //start server and bind its local and remote API
            var cts = new CancellationTokenSource();
            Server.ListenAsync("http://localhost:9001/", cts.Token, (c, ws) => c.Bind(new BackgroundService())).Wait(0);

            Console.Write("Running: '{0}'. Press [Enter] to exit.", nameof(BackgroundService));
            Console.ReadLine();
            cts.Cancel();
        }
    }
}
