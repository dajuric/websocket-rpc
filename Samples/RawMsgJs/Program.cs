using SampleBase;
using System;
using System.IO;
using System.Threading;
using WebSocketRPC;

namespace RawMsgJs
{
    //Empty class (would contains methods if RPC was used).
    class MessagingAPI
    { }

    class Program
    {
        //if access denied execute: "netsh http delete urlacl url=http://+:8001/" (delete for 'localhost', add for public address)
        //open Index.html to run the client
        static void Main(string[] args)
        {
            //set message limit
            Connection.MaxMessageSize = Connection.Encoding.GetMaxByteCount(40);

            //generate js code
            File.WriteAllText($"./Site/{nameof(MessagingAPI)}.js", RPCJs.GenerateCaller<MessagingAPI>());
          
            //start server
            var cts = new CancellationTokenSource();
            var t = Server.ListenAsync("http://localhost:8001/", cts.Token, (c, ws) =>
            {
                //set idle timeout 
                c.BindTimeout(TimeSpan.FromSeconds(30));

                c.OnOpen += async () => await c.SendAsync("Hello from server using WebSocketRPC");
                c.OnClose += () => Console.WriteLine("Connection closed.");
                c.OnError += e => Console.WriteLine("Error: " + e.Message);
                
                c.OnReceive += async msg =>
                {
                    Console.WriteLine("Received: " + msg);

                    await c.SendAsync("Server received: " + msg);

                    if (msg.ToLower() == "close")
                        await c.CloseAsync(statusDescription: "Close requested by user.");
                };
            });

            Console.Write("{0} ", nameof(RawMsgJs));
            AppExit.WaitFor(cts, t);
        }
    }
}
