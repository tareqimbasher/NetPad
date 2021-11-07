using System.Threading.Tasks;
using ElectronNET.API;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.HttpsPolicy;
using Microsoft.AspNetCore.SpaServices.AngularCli;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using NetPad.Queries;
using NetPad.Sessions;
using Session = ElectronNET.API.Session;

namespace NetPad
{
    public class Startup
    {
        public Startup(IConfiguration configuration)
        {
            Configuration = configuration;
        }

        public IConfiguration Configuration { get; }

        public void ConfigureServices(IServiceCollection services)
        {
            services.AddControllersWithViews();
            // In production, the SPA files will be served from this directory
            services.AddSpaStaticFiles(configuration => { configuration.RootPath = "App/dist"; });
            
            services.AddSingleton(Configuration.GetSection("Settings").Get<Settings>());
            services.AddSingleton<ISession, NetPad.Sessions.Session>();
            services.AddSingleton<IQueryManager, QueryManager>();
        }

        public void Configure(IApplicationBuilder app, IWebHostEnvironment env)
        {
            if (env.IsDevelopment())
            {
                app.UseDeveloperExceptionPage();
            }
            else
            {
                app.UseExceptionHandler("/Error");
                // The default HSTS value is 30 days. You may want to change this for production scenarios, see https://aka.ms/aspnetcore-hsts.
                app.UseHsts();
            }

            app.UseHttpsRedirection();
            app.UseStaticFiles();
            if (!env.IsDevelopment())
            {
                app.UseSpaStaticFiles();
            }

            app.UseRouting();

            app.UseEndpoints(endpoints =>
            {
                endpoints.MapControllerRoute(
                    name: "default",
                    pattern: "{controller}/{action=Index}/{id?}");
            });

            app.UseSpa(spa =>
            {
                // To learn more about options for serving an Angular SPA from ASP.NET Core,
                // see https://go.microsoft.com/fwlink/?linkid=864501

                spa.Options.SourcePath = "App";

                if (env.IsDevelopment())
                {
                    spa.UseProxyToSpaDevelopmentServer("http://localhost:9000/");
                }
            });

            if (HybridSupport.IsElectronActive)
            {
                Task.Run(async () => await Electron.WindowManager.CreateWindowAsync());
                
                // var options = new BrowserWindowOptions()
                // {
                //     Show = false
                // };
                //
                // Task.Run(async () =>
                // {
                //     var browserWindow = await Electron.WindowManager.CreateWindowAsync(options);
                //     browserWindow.OnShow += () => browserWindow.Show();
                // });
            }
        }
    }
}