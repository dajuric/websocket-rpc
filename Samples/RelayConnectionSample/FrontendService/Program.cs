using System;
using System.IO;
using System.Threading;
using WebsocketRPC;

namespace FrontendService
{
    /// <summary>
    /// Background service contract.
    /// </summary>
    public interface IBackgroundService
    {
        /// <summary>
        /// Concatenates two strings.
        /// </summary>
        /// <param name="a">First string.</param>
        /// <param name="b">Second string.</param>
        /// <returns>Concatenated string.</returns>
        string ConcatString(string a, string b);
    }

    public class Program
    {
        //if access denied execute: "netsh http delete urlacl url=http://+:8001/"
        static void Main(string[] args)
        {
            //generate js code
            File.WriteAllText($"../Site/{nameof(IBackgroundService)}.js", RPCJs.GenerateCallerWithDoc<IBackgroundService>());

            //start server and bind its local and remote API
            var cts = new CancellationTokenSource();
            Client.ConnectAsync("ws://localhost:9001/", cts.Token, c => c.Bind<IBackgroundService>()).Wait(0); //this -> background
            Server.ListenAsync("http://localhost:8001/", cts.Token, (c, ws) => c.Relay<IBackgroundService>()).Wait(0); //client -> background rely

            Console.Write("Running: '{0}'. Press [Enter] to exit.", nameof(FrontendService));
            Console.ReadLine();
            cts.Cancel();
        }
    }
}
