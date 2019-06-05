using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Itinero;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.HttpsPolicy;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace LiveTrafficServer
{
    public class Startup
    {

        public static RouterDb routerDb;
        public Startup(IConfiguration configuration)
        {
            Configuration = configuration;
            routerDb = new RouterDb();
            while (true)
            {
                try
                {
                    using (var stream = System.IO.File.OpenRead(CommonVariables.PathToCommonFolder + CommonVariables.RouterDbFileName))
                    {
                        routerDb = RouterDb.Deserialize(stream);
                    }
                    break;
                }
                catch (Exception e)
                {
                    Console.WriteLine(e.ToString());
                }
            }
        }

        public IConfiguration Configuration { get; }

        // This method gets called by the runtime. Use this method to add services to the container.
        public void ConfigureServices(IServiceCollection services)
        {
            services.AddMvc().SetCompatibilityVersion(CompatibilityVersion.Version_2_1);
        }

        // This method gets called by the runtime. Use this method to configure the HTTP request pipeline.
        public void Configure(IApplicationBuilder app, IHostingEnvironment env)
        {
            if (env.IsDevelopment())
            {
                app.UseDeveloperExceptionPage();
            }
            else
            {
                app.UseHsts();
            }

            app.UseHttpsRedirection();
            app.UseMvc();
        }
    }
}
