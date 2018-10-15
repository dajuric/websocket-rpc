using System;
using System.Collections.Generic;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;

namespace WebSocketRPC
{
    public class WebSocketRPCOptions : WebSocketOptions
    {
        public Dictionary<PathString, Action<HttpContext, Connection>> Bindings;

        public WebSocketRPCOptions(Action<HttpContext, Connection> onConnect)
            : this()
        {
            Bindings.Add(string.Empty, onConnect);
        }

        public WebSocketRPCOptions(PathString path, Action<HttpContext, Connection> onConnect)
            : this()
        {
            Bindings.Add(path, onConnect);
        }

        private WebSocketRPCOptions()
        {
            Bindings = new Dictionary<PathString, Action<HttpContext, Connection>>();
        }
    }
}