using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using System;

namespace WebSocketRPC
{ 
    /// <summary>
    /// WebSocket RPC middle-ware extensions.
    /// </summary>
    public static class WebSokcetRPCMiddlewareExtensions
    {
        /// <summary>
        /// Branches the request pipeline based on matches of the given request path. If  the request path starts with the given path, the branch is executed.
        /// </summary>
        /// <param name="app">Application builder.</param>
        /// <param name="path">The request path to match.</param>
        /// <param name="onConnection">Action triggered when a new connection is established.</param>
        /// <returns>Application builder.</returns>
        public static IApplicationBuilder MapWebSocketRPC(this IApplicationBuilder app,
                                                          PathString path,
                                                          Action<HttpContext, Connection> onConnection)
        {
            return app.Map(path, (_app) => _app.UseMiddleware<WebSocketRPCMiddleware>(onConnection));
        }

        /// <summary>
        /// Adds a WebSocketRPC middleware to the application's request pipeline.
        /// </summary>
        /// <param name="app"></param>
        /// <param name="onConnection"></param>
        /// <returns>Application builder.</returns>
        public static IApplicationBuilder UseWebSocketRPC(this IApplicationBuilder app, Action<HttpContext, Connection> onConnection)
        {
           return app.UseMiddleware<WebSocketRPCMiddleware>(onConnection);
        }
    }
}
