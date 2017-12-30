<p align="center">
    <a href="https://www.nuget.org/profiles/dajuric"> <img src="Deploy/Logo/Logo-big.png" alt="WebSocketRPC logo" width="120" align="center"> </a>
</p>

<p align="center">
    <a href="https://www.nuget.org/packages/WebsocketRPC/"> <img src="https://img.shields.io/badge/WebSokcetRPC-v1.x-blue.svg?style=flat-square" alt="NuGet packages version"/>  </a>
    <a href="https://www.nuget.org/packages/WebsocketRPC.JS/"> <img src="https://img.shields.io/badge/WebSokcetRPC.JS-v1.x-blue.svg?style=flat-square" alt="NuGet packages version"/>  </a>
    <a href="https://www.nuget.org/packages/WebsocketRPC.AspCore/"> <img src="https://img.shields.io/badge/WebSokcetRPC.AspCore-v1.x-blue.svg?style=flat-square" alt="NuGet packages version"/>  </a>
</p>

**WebSokcetRPC** - RPC over websocket for .NET    
Lightweight .NET framework for making RPC over websockets. Supports full duplex connections; .NET or Javascript clients. 

<!--
 > **Tutorial:** <a href="https://www.codeproject.com/Articles/0000/Introducing-Leightweight-WebSocket-RPC-library" target="_blank">CodeProject article</a>
-->


## Why WebSocketRPC ?

+ **Lightweight**   
The only dependency is <a href="https://www.newtonsoft.com/json">JSON.NET</a> library used for serialization/deserialization.

+ **Simple**   
There are two relevant method: **Bind** for binding object/interface onto connection, and **CallAsync** for making RPCs.

+ **Use 3rdParty assemblies as API(s)**   
Implemented API, if used only for RPC, does not use anything from the library.

+ **Automatic Javascript code generation** *(WebSocketRPC.JS package)*  
 Javascript websocket client code is automatically generated **_(with JsDoc comments)_** from an existing .NET
                        interface (API contract).

 
## <a href="Samples/"> Samples</a>

Check the samples by following the link above. The snippets below demonstrate the base functionality.

#### 1) .NET <-> .NET (raw messaging)
The sample demonstrates communication between server and client using raw messaging. The server relays client messages.

**Server** (C#)
 ``` csharp
Server.ListenAsync("http://localhost:8000/", CancellationToken.None, (c, wc) => 
{
    c.OnOpen  += ()                    => Task.Run(() => Console.WriteLine("Opened"));
    c.OnClose += (status, description) => Task.Run(() => Console.WriteLine("Closed: " + description));
    c.OnError += ex                    => Task.Run(() => Console.WriteLine("Error: " + ex.Message));     

    c.OnReceive += msg                 => await c.SendAsync("Received: " + msg); //relay message
})
.Wait(0);
 ``` 

 **Client** (C#)
  ``` csharp
Client.ListenAsync("ws://localhost:8000/", CancellationToken.None, c => 
{
      c.OnOpen += () => await c.SendAsync("Hello from a client");
},
reconnectOnError: true)
.Wait(0);
 ```
{empty} . 


#### 2) .NET <-> .NET (RPC)
A data aggregator service is built. The server gets the multiple number sequences of each client and sums all the numbers.  
The procedure is repeated for each new client connection.

**Server** (C#)
 ``` csharp
//client API contract
interface IClientAPI
{
    int[] GetLocalNumbers();
}

....
async Task WriteTotalSum()
{  
    //get all the clients (notice there is no 'this' (the sample above))
    var clients = RPC.For<IClientAPI>();

    //get the numbers sequences
    var numberGroups = await clients.CallAsync(x => x.GetLocalNumbers());
    //flatten the collection and sum all the elements
    var sum = numberGroups.SelectMany(x => x).Sum();

    Console.WriteLine("Client count: {0}; sum: {1}.", clients.Count(), sum);
}

//run server
Server.ListenAsync("http://localhost:8000/", CancellationToken.None, 
                   (c, wc) => 
                   { 
                       c.Bind<IClientAPI>();
                       c.OnOpen += WriteTotalSum;
                   })
                   .Wait(0);

/*
Output: 
   Client count: 1; sum: 4.
   Client count: 3; sum: 14.
   ...
*/
 ``` 
 
**Client** (C#)
``` csharp
//client API
class ClientAPI
{
    int[] GetLocalNumbers()
    {
       var r = new Random();
      
       var numbers = new int[10];
       for(var i = 0; i < numbers.Length; i++)
          numbers[i] = r.Next();

       return numbers;
    }
}

....
//run client
Client.ListenAsync("ws://localhost:8000/", CancellationToken.None, 
                   c => c.Bind(new ClientAPI())).Wait(0);
 ``` 
{empty} .  

#### 3) .NET <-> Javascript (RPC)
Simple math service is built and invoked remotely. The math service has a single long running method which adds two numbers (server side).
Client calls the method and receives progress update until the result does not become available.

**Server** (C#)
 ``` csharp
//client API contract
interface IReportAPI
{
    void WriteProgress(int progress);
}

//server API
class MathAPI
{
    public async Task<int> LongRunningTask(int a, int b)
    {
        for (var p = 0; p <= 100; p += 5)
        {
            await Task.Delay(250);
            //update only the client which called this method (hence 'this')
            await RPC.For<IReportAPI>(this).CallAsync(x => x.WriteProgress(p));
        }

        return a + b;
    }
}

....
//generate js code with JsDoc documentation taken from XML comments (if any)
File.WriteAllText("MathAPI.js", RPCJs.GenerateCallerWithDoc<MathAPI>());
//run server
Server.ListenAsync("http://localhost:8000/", CancellationToken.None, 
                    (c, wc) => c.Bind<MathAPI, IReportAPI>(new MathAPI())).Wait(0);
 ``` 

 **Client** (Javascript)
  ``` javascript
//init 'MathAPI'
var api = new MathAPI("ws://localhost:8000");
//implement the 'IReportAPI'
api.writeProgress = p => console.log("Progress: " + p + "%");

//connect to the server and call the remote function
api.connect(async () => 
{
    var r = await api.longRunningTask(5, 3);
    console.log("Result: " + r);
});

/*
  Output: 
     Progress: 0 %
     Progress: 5 %
     Progress: 10 %
     ...
     Result: 8
*/
 ``` 
  
{empty} .  


## Getting started
+ Samples
<!--
+ <a href="https://www.codeproject.com/Articles/0000/Introducing-Leightweight-WebSocket-RPC-library" target="_blank">CodeProject article</a>
-->

## How to Engage, Contribute and Provide Feedback  
Remember: Your opinion is important and will define the future roadmap.
+ questions, comments - Github
+ **spread the word** 

## Final word
If you like the project please **star it** in order to help to spread the word. That way you will make the framework more significant and in the same time you will motivate me to improve it, so the benefit is mutual.
