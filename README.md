<p align="center">
    <a href="https://www.nuget.org/profiles/dajuric"> <img src="Deploy/Logo/Logo-big.png" alt="WebSocketRPC logo" width="120" align="center"> </a>
</p>

<!--
<p align="center">
    <a href="https://www.nuget.org/profiles/dajuric"> <img src="https://img.shields.io/badge/WebSokcetRPC-v1.0.1-blue.svg?style=flat-square" alt="NuGet packages version"/>  </a>
    <a href="https://www.nuget.org/profiles/dajuric"> <img src="https://img.shields.io/badge/WebSokcetRPC.JS-v1.0.1-blue.svg?style=flat-square" alt="NuGet packages version"/>  </a>
</p>
-->

**WebSokcetRPC** - RPC over weboskcets for .NET    
Leightweight .NET framework for making RPC over websockets. Supports full duplex connections; .NET or Javascript clients. 

<!-- > **Tutorial:** <a href="http://www.codeproject.com/Articles/828012/Introducing-Lightweight-Weboskcet-RPC-library-for-Csharp" target="_blank">CodeProject article</a>-->


## Why WebSocketRPC ?

+ **Lightweight**   
The only dependency is <a href="https://www.newtonsoft.com/json">JSON.NET</a> library used for serialization/deserialization.

+ **Simple**   
There is only one relevant method: **Bind** for binding object/interface onto connection, and **CallAsync** for calling RPCs.

+ **Connection relaying in a single line of code**   
 The library is capable to relay incoming requests to a background services with just one command: **Relay**

+ **Automatic Javascript code generation** *(WebsocketRPC.JS package)*  
 Javascript websokcet client code is automatically generated (with JsDoc comments) from an existing .NET
                        interface (API contract).

 
## Sample

To scratch the surface... *(RPC in both directions, multi-service, .NET clients)*

**Server**
 ``` csharp
class MathAPI
{
    public async Task<int> LongRunningTask(int a, int b)
    {
        await Task.Delay(250);
        return a + b;
    }
}

....
//generate js code
File.WriteAllText("MathAPI.js", RPCJs.GenerateCallerWithDoc<MathAPI>());
//run server
Server.ListenAsync("http://localhost:8000/", CancellationToken.None, 
                    (c, wc) => c.Bind<MathAPI>(new MathAPI()))
       .Wait(0);
 ``` 

 **Client**
  ``` javascript
var api = new MathAPI("ws://localhost:8000");
api.connect(async () => 
{
    var r = await api.longRunningTask(5, 3);
    console.log("Result: " + r);
});
 ``` 
  
 
## Getting started
+ Samples
<!--+ <a href="http://www.codeproject.com/Articles/828012/Introducing-Lightweight-Weboskcet-RPC-library-for-Csharp" target="_blank">CodeProject article</a>-->

## How to Engage, Contribute and Provide Feedback  
Remember: Your opinion is important and will define the future roadmap.
+ questions, comments - message on Github, or write to: darko.juric2 [at] gmail.com
+ **spread the word** 

## Final word
If you like the project please **star it** in order to help to spread the word. That way you will make the framework more significant and in the same time you will motivate me to improve it, so the benefit is mutual.
