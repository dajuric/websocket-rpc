using Microsoft.AspNetCore;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.FileProviders;
using System.IO;
using WebSocketRPC;

namespace AspRpc
{
    class Startup
    {
        ReportingService reportingService = null;

        public Startup(IConfiguration configuration)
        {
            Configuration = configuration;
        }

        public IConfiguration Configuration { get; }

        public void ConfigureServices(IServiceCollection services)
        {
            services.AddMvcCore();
        }

        public void Configure(IApplicationBuilder app, IHostingEnvironment env)
        {
            if (env.IsDevelopment())
            {
                app.UseDeveloperExceptionPage();
            }
  
            app.UseMvc();
            app.UseStaticFiles(new StaticFileOptions
            {
                FileProvider = new PhysicalFileProvider(Path.Combine(Directory.GetCurrentDirectory(), "Site")),
                RequestPath = "/Site"
            });

            //initialize web-sockets and bind the service
            reportingService = new ReportingService();
            app.UseWebSockets();
            app.MapWebSocketRPC("/reportingService", (hc, c) => c.Bind<ReportingService, IClientUpdate>(reportingService));
        }
    }

    class Program
    {
        public static void Main(string[] args)
        {
            //generate js code
            File.WriteAllText($"./Site/{nameof(ReportingService)}.js", RPCJs.GenerateCallerWithDoc<ReportingService>());

            WebHost.CreateDefaultBuilder(args)
                   .UseStartup<Startup>()
                   .Build()
                   .Run();
        }
    }
}
