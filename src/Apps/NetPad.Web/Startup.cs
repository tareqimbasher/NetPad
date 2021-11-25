using System;
using System.IO;
using System.Linq;
using System.Text.Json.Serialization;
using System.Threading.Tasks;
using ElectronNET.API;
using ElectronNET.API.Entities;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
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
using NetPad.Utils;
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
            ElectronUtil.Initialize("http://localhost:8001");
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

            services.AddSingleton(Configuration.GetSection("Settings").Get<Settings>());
            services.AddSingleton<ISession, Sessions.Session>();
            services.AddSingleton<IScriptRepository, FileSystemScriptRepository>();
            services.AddTransient<IUiScriptService, UiScriptService>();

            services.AddTransient<ICodeParser, CSharpCodeParser>();
            services.AddTransient<ICodeCompiler, CSharpCodeCompiler>();
            services.AddTransient<IAssemblyLoader, MainAppDomainAssemblyLoader>();
            services.AddTransient<IScriptRuntime, ScriptRuntime>();

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
                Task.Run(async () =>
                {
                    var display = await Electron.Screen.GetPrimaryDisplayAsync();

                    await ElectronUtil.CreateWindowAsync("main", new BrowserWindowOptions
                    {
                        Height = display.Bounds.Height * 2 / 3,
                        Width = display.Bounds.Width * 2 / 3,
                        MinHeight = 200,
                        MinWidth = 200,
                        Center = true
                    });
                });

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

        private void AddSwagger(IServiceCollection services)
        {
            services.AddSwaggerDocument(config =>
            {
                config.Title = "NetPad";
                config.PostProcess = document =>
                {
                    var settings = new TypeScriptClientGeneratorSettings
                    {
                        ClassName = "{controller}Service",
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
