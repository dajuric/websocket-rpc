using SampleBase;
using System;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using WebSocketRPC;

namespace ClientInfoJs
{
    class PlatformInfo
    {
        internal async Task InitializeAsync()
        {
            var browserInfo = await RPC.For<IBrowserInfo>(this)
                                       .CallAsync(x => x.GetBrowserInfo());

            Console.WriteLine("\nBrowser info: " + browserInfo.First());
        }

        /* There are no public methods so the API is 'empty' from a client's perspective */
    }

    interface IBrowserInfo
    {
        string GetBrowserInfo();
    }

    class Program
    {
        //if access denied execute: "netsh http delete urlacl url=http://+:8001/" (delete for 'localhost', add for public address)
        //open Index.html to run the client
        static void Main(string[] args)
        {
            //generate js code (the API is empty)
            File.WriteAllText($"./Site/{nameof(PlatformInfo)}.js", RPCJs.GenerateCaller<PlatformInfo>());
          
            //start server and bind its local and remote API
            var cts = new CancellationTokenSource();
            var t = Server.ListenAsync("http://localhost:8001/", cts.Token, (c, ws) =>
            {                
                var pInfo = new PlatformInfo();
                c.Bind<PlatformInfo, IBrowserInfo>(pInfo);
                c.OnOpen += pInfo.InitializeAsync;
            });

            Console.Write("{0} ", nameof(ClientInfoJs));
            Process.Start(new ProcessStartInfo(Path.GetFullPath("./Site/Index.html")) { UseShellExecute= true });
            AppExit.WaitFor(cts, t);
        }
    }
}
