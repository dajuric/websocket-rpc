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

 > **Tutorial:** <a href="https://www.codeproject.com/Articles/1210957/Introducing-Lightweight-WebSocket-RPC-Library-for" target="_blank">CodeProject article</a>


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

#### 1) .NET <-> .NET (RPC)
The server's *TaskAPI* has a function which during its execution updates progress and reports it only to clients which called the method.

**Server** (C#)
 ``` csharp
 //client's API contract
interface IProgressAPI
{
   void WriteProgress(float progress);
}

//server's API
class TaskAPI  //:ITaskAPI
{
   public async Task<int> LongRunningTask(int a, int b) {
      for (var p = 0; p <= 100; p += 5) {
         await Task.Delay(250);
         //select only those connections which are associated with 'IProgressAPI' and with 'this' object.
         await RPC.For<IProgressAPI>(this).CallAsync(x => x.WriteProgress((float)p / 100));
      }
		
      return a + b;
   }
}

...
//run the server and bind the local and remote API to a connection
Server.ListenAsync(8000, CancellationToken.None, 
                   (c, wc) => c.Bind<TaskAPI, IProgressAPI>(new TaskAPI()))
       .Wait(0);
 ``` 
 
**Client** (C#)
``` csharp
//client's API
class ProgressAPI //:IProgressAPI
{
   void WriteProgress(float progress) {
       Console.Write("Completed: " + progress * 100 + "%\r");
   }
}

//server's API contract
interface ITaskAPI {
   Task<int> LongRunningTask(int a, int b);
}

...
//run the client and bind the APIs to the connection
Client.ConnectAsync("ws://localhost:8000/", CancellationToken.None, 
                    (c, wc) => c.Bind<ProgressAPI, ITaskAPI>(new ProgressAPI()))
      .Wait(0);
...
//make an RPC
var r = await RPC.For<ITaskAPI>().CallAsync(x => LongRunningTask(5, 3)); 
Console.WriteLine("Result: " + r.First());

/*
 Output:
   Completed: 0%
   Completed: 5%
     ...
   Completed: 100%
   Result: 8
*/ 
 ``` 

#### 2) .NET <-> Javascript (RPC)
Let us use the same server implementation as in the two-way binding sample, but this time the client will be written in JavaScript.

**Server** (C#)
 ``` csharp
//the server code is the same as in the previous sample

//generate JavaScript client (file)
var code = RPCJs.GenerateCallerWithDoc<TaskAPI>();
File.WriteAllText("TaskAPI.js", code);
 ``` 

 **Client** (Javascript)
  ``` javascript
//init API
var api = new TaskAPI("ws://localhost:8001");

//implement the interface by extending the 'TaskAPI' object
api.writeProgress = function (p) {
     console.log("Completed: " + p * 100 + "%");
     return true;
}

//connect and excecute (when connection is opened)
api.connect(async () => {
     var r = await api.longRunningTask(5, 3);
     console.log("Result: " + r);
});
 ``` 
 
#### 3) ASP.NET Core
 ``` csharp
class Startup
{
    public void Configure(IApplicationBuilder app, IHostingEnvironment env){
        //the MVC initialization, etc.

        //initialize web-sockets
        app.UseWebSockets();
        //define route for a new connection and bind the API
        app.MapWebSocketRPC("/taskAPI", (httpCtx, c) => c.Bind<TaskAPI, IProgressAPI>(new TaskAPI()));
    }
}  
 ```
  
## How to Engage, Contribute and Provide Feedback  
Remember: Your opinion is important and will define the future roadmap.
+ questions, comments - Github
+ **spread the word** 

## Final word
If you like the project please **star it** in order to help to spread the word. That way you will make the framework more significant and in the same time you will motivate me to improve it, so the benefit is mutual.
