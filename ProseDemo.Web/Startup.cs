using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using JetBrains.Annotations;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc.ViewFeatures;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace ProseDemo.Web {
    [UsedImplicitly]
    public class Startup {
        // This method gets called by the runtime. Use this method to add services to the container.
        // For more information on how to configure your application, visit https://go.microsoft.com/fwlink/?LinkID=398940
        public void ConfigureServices(IServiceCollection services) {
            services.AddResponseCompression()
                    .AddDistributedMemoryCache()
                    .AddRouting()
                    .AddSession()
                    .AddCors()
                    .AddMvc();
            services.AddSingleton<ITempDataProvider, SessionStateTempDataProvider>();
        }

        // This method gets called by the runtime. Use this method to configure the HTTP request pipeline.
        public void Configure(IApplicationBuilder app, IHostingEnvironment env, ILoggerFactory loggerFactory) {
            loggerFactory.AddConsole()
                         .AddDebug();

            if (env.IsDevelopment()) {
                app.UseDeveloperExceptionPage()
                   .UseBrowserLink();
            }
            app.UseStaticFiles()
               .UseSession()
               .UseResponseCompression()
               .UseMvc(routes => { routes.MapRoute("default", "{controller=Home}/{action=Index}/"); });
        }
    }
}
