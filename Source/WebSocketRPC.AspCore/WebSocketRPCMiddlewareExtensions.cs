using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using System;

namespace WebSocketRPC
{ 
    /// <summary>
    /// WebSocket RPC middle-ware extensions.
    /// </summary>
    public static class WebSocketRPCMiddlewareExtensions
    {
        /// <summary>
        /// Branches the request pipeline based on matches of the given request path. If  the request path starts with the given path, the branch is executed.
        /// <para>Make sure the 'UseWebSockets()' from Microsoft.AspNetCore.WebSockets is called before.</para>
        /// </summary>
        /// <param name="app">Application builder.</param>
        /// <param name="path">The request path to match.</param>
        /// <param name="onConnect">Action triggered when a new connection is established.</param>
        /// <returns>Application builder.</returns>
        public static IApplicationBuilder MapWebSocketRPC(this IApplicationBuilder app,
                                                          PathString path,
                                                          Action<HttpContext, Connection> onConnect)
        {
            return app.Map(path, (_app) => _app.UseMiddleware<WebSocketRPCMiddleware>(onConnect));
        }

        /// <summary>
        /// Adds a WebSocketRPC middleware to the application's request pipeline.
        /// <para>Make sure the 'UseWebSockets()' from Microsoft.AspNetCore.WebSockets is called before.</para>
        /// </summary>
        /// <param name="app">Application builder.</param>
        /// <param name="onConnect">Action triggered when a new connection is established.</param>
        /// <returns>Application builder.</returns>
        public static IApplicationBuilder UseWebSocketRPC(this IApplicationBuilder app, Action<HttpContext, Connection> onConnect)
        {
           return app.UseMiddleware<WebSocketRPCMiddleware>(onConnect);
        }
    }
}
