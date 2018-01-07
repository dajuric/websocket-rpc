<p align="center">
    <img src="Deploy/Logo/Logo-big.png" alt="WebSocketRPC logo" width="120" align="center" />
</p>

<p align="center">
    <a href="https://www.nuget.org/packages/WebsocketRPC.Standalone/"> <img src="https://img.shields.io/badge/WebSokcetRPC.Standalone-v1.x-blue.svg?style=flat-square" alt="NuGet packages version"/>  </a>
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
There are only two relevant methods: **Bind** for binding object/interface onto a connection, and **CallAsync** for making RPCs.

+ **Use 3rdParty assemblies as API(s)**   
Implemented API, *if used only for RPC*, does not use anything from the library.

+ **Automatic JavaScript code generation**  
 The JavaScript WebSocket client code is automatically generated **_(with JsDoc comments)_** from an existing .NET interface (API contract).

 
## <a href="Samples/"> Samples</a>

Check the samples by following the link above. The snippets below demonstrate the base RPC functionality.

#### 1) .NET <- .NET
The server implements a math API containing a single function.

**Server** (C#)
 ``` csharp
//server's API
class MathAPI //:IMathAPI
{
     public int Add(int a, int b)
     {
         return a + b;
     }
}

...
//run the server and bind the local and remote API to a connection
Server.ListenAsync(8000, CancellationToken.None, 
                    (c, wc) => c.Bind<MathAPI>(new MathAPI()))
       .Wait(0);
 ``` 
 
**Client** (C#)
``` csharp
//server's API contract
interface IMathAPI
{
    int Add(int a, int b);
}

...
//run the client and bind the APIs to the connection
Client.ConnectAsync("ws://localhost:8000/", CancellationToken.None, 
                    (c, ws) => c.Bind<IMathAPI>())
      .Wait(0);
      
...
//make an RPC (there is only one connection)
var r = await RPC.For<IMathAPI>().CallAsync(x => Add(5, 3)); 
Console.WriteLine("Result: " + r.First()); //Output: 'Result: 8'
 ``` 

#### 2) .NET <- Javascript
The server's code is the same, but the client is written in JavaScript. The support is given by the *WebSocketRPC.JS* package.

**Server** (C#)
 ``` csharp
//the server code is the same as in the previous sample

//generate JavaScript client (file)
var code = RPCJs.GenerateCallerWithDoc<MathAPI>();
File.WriteAllText("MathAPI.js", code);
 ``` 

 **Client** (Javascript)
  ``` javascript
//init API
var api = new MathAPI("ws://localhost:8000");

//connect and excecute (when connection is opened)
api.connect(async () => {
     var r = await api.add(5, 3);
     console.log("Result: " + r);
});
 ``` 
 
#### 3) ASP.NET Core
To incorporate server's code into the ASP.NET Core use *WebSocketRPC.AspCore* package. The initialization is done in a startup class in the *Configure* method. Everything the rest is the same.

 ``` csharp
class Startup
{
     public void Configure(IApplicationBuilder app, IHostingEnvironment env) 
     {
         //the MVC initialization, etc.

         //initialize web-sockets
         app.UseWebSockets();
         //define route for a new connection and bind the API
         app.MapWebSocketRPC("/mathAPI", (httpCtx, c) => c.Bind<MathAPI>(new MathAPI()));
     }
}  
 ```
  
## Related Libraries
<a href="https://github.com/dajuric/simple-http" target="_blank">SimpleHTTP library</a>


## How to Engage, Contribute and Provide Feedback  
Remember: Your opinion is important and will define the future roadmap.
+ questions, comments - Github
+ **spread the word** 

## Final word
If you like the project please **star it** in order to help to spread the word. That way you will make the framework more significant and in the same time you will motivate me to improve it, so the benefit is mutual.
