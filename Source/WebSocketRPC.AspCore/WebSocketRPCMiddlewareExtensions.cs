using Microsoft.AspNetCore.Builder;

namespace WebSocketRPC
{
    /// <summary>
    /// WebSocket RPC middle-ware extensions.
    /// </summary>
    public static class WebSocketRPCMiddlewareExtensions
    {
        /// <summary>
        /// Adds a WebSocketRPC middleware to the application's request pipeline.
        /// </summary>
        /// <param name="app">Application builder.</param>
        /// <param name="options">Options for WebSocketRPC calls.</param>
        /// <returns>Application builder.</returns>
        public static IApplicationBuilder UseWebSocketRPC(this IApplicationBuilder app, WebSocketRPCOptions options)
        {
            app.UseWebSockets(options);

            foreach (var binding in options.Bindings)
            {
                if (string.IsNullOrWhiteSpace(binding.Key))
                {
                    app.UseMiddleware<WebSocketRPCMiddleware>(binding.Value);
                }
                else
                {
                    app.Map(binding.Key, (x) => x.UseMiddleware<WebSocketRPCMiddleware>(binding.Value));
                }
            }

            return app;
        }
    }
}
