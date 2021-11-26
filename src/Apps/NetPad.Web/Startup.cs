using System;
using System.IO;
using System.Linq;
using System.Text.Json.Serialization;
using System.Threading.Tasks;
using ElectronNET.API;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Hosting.Server.Features;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using NetPad.BackgroundServices;
using NetPad.Compilation;
using NetPad.Compilation.CSharp;
using NetPad.Middlewares;
using NetPad.Scripts;
using NetPad.Runtimes;
using NetPad.Runtimes.Assemblies;
using NetPad.Services;
using NetPad.Sessions;
using NetPad.UiInterop;
using NJsonSchema.CodeGeneration.TypeScript;
using NSwag.CodeGeneration.TypeScript;

namespace NetPad
{
    public class Startup
    {
        public Startup(IConfiguration configuration, IWebHostEnvironment webHostEnvironment)
        {
            Configuration = configuration;
            WebHostEnvironment = webHostEnvironment;
        }

        public IConfiguration Configuration { get; }
        public IWebHostEnvironment WebHostEnvironment { get; }

        public void ConfigureServices(IServiceCollection services)
        {
            services.AddControllersWithViews().AddJsonOptions(config =>
            {
                config.JsonSerializerOptions.Converters.Add(new JsonStringEnumConverter());
            });

            // In production, the SPA files will be served from this directory
            services.AddSpaStaticFiles(configuration => { configuration.RootPath = "App/dist"; });

            if (WebHostEnvironment.IsDevelopment())
            {
                AddSwagger(services);
            }

            services.AddSingleton<HostInfo>();
            services.AddSingleton(Configuration.GetSection("Settings").Get<Settings>());

            services.AddSingleton<ISession, Sessions.Session>();
            services.AddSingleton<IScriptRepository, FileSystemScriptRepository>();

            services.AddTransient<ICodeParser, CSharpCodeParser>();
            services.AddTransient<ICodeCompiler, CSharpCodeCompiler>();
            services.AddTransient<IAssemblyLoader, MainAppDomainAssemblyLoader>();
            services.AddTransient<IScriptRuntime, ScriptRuntime>();

            services.AddTransient<IUiDialogService, ElectronDialogService>();
            services.AddTransient<IUiWindowService, ElectronWindowService>();
            services.AddTransient<IIpcService, ElectronIpcService>();

            services.AddHostedService<SessionBackgroundService>();
            services.AddHostedService<ScriptBackgroundService>();
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
            else
            {
                app.UseOpenApi();
                app.UseSwaggerUi3();
            }

            app.UseMiddleware<ExceptionHandlerMiddleware>();

            app.UseRouting();

            app.UseEndpoints(endpoints =>
            {
                endpoints.MapControllerRoute(
                    name: "default",
                    pattern: "{controller}/{action=Index}/{id?}");
            });

            app.UseSpa(spa =>
            {
                // To learn more about options for serving an SPA from ASP.NET Core,
                // see https://go.microsoft.com/fwlink/?linkid=864501

                spa.Options.SourcePath = "App";

                if (env.IsDevelopment())
                {
                    spa.UseProxyToSpaDevelopmentServer("http://localhost:9000/");
                }
            });

            app.ApplicationServices.GetRequiredService<HostInfo>().SetHostUrl(app.ServerFeatures
                    .Get<IServerAddressesFeature>()
                    .Addresses
                    .First(a => a.StartsWith("http:")));

            if (HybridSupport.IsElectronActive)
            {
                Task.Run(async () =>
                {
                    await app.ApplicationServices.GetRequiredService<IUiWindowService>().OpenMainWindowAsync();
                });
            }
        }

        private void AddSwagger(IServiceCollection services)
        {
            services.AddSwaggerDocument(config =>
            {
                config.Title = "NetPad";
                config.PostProcess = document =>
                {
                    var settings = new TypeScriptClientGeneratorSettings
                    {
                        ClassName = "{controller}ApiClient",
                        Template = TypeScriptTemplate.Aurelia,
                        GenerateClientInterfaces = true,
                        QueryNullValue = null,
                        UseAbortSignal = true,
                        TypeScriptGeneratorSettings =
                        {
                            EnumStyle = TypeScriptEnumStyle.StringLiteral,
                            GenerateCloneMethod = true,
                            TypeScriptVersion = 4.4m
                        }
                    };

                    var generator = new TypeScriptClientGenerator(document, settings);

                    var lines = generator.GenerateFile()
                        .Replace("private http: { fetch(url: RequestInfo, init?: RequestInit): Promise<Response> };", "private http: IHttpClient;")
                        .Replace("http?: { fetch(url: RequestInfo, init?: RequestInit): Promise<Response> }", "@IHttpClient http?: IHttpClient")
                        .Split(Environment.NewLine)
                        .Where(l => !l.StartsWith("import") && !l.StartsWith("@inject"))
                        .ToList();

                    lines.Insert(9, "import {IHttpClient} from \"aurelia\";");

                    File.WriteAllText(
                        Path.Combine(WebHostEnvironment.ContentRootPath, "App", "src", "core", "@domain", "api.ts"),
                        string.Join(Environment.NewLine, lines));
                };
            });
        }
    }
}
