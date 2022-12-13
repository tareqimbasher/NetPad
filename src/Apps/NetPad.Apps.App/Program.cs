using System;
using System.Linq;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Hosting;
using NetPad.Configuration;
using NetPad.Electron;
using NetPad.Web;
using Serilog;

namespace NetPad
{
    public static class Program
    {
        internal static IApplicationConfigurator ApplicationConfigurator { get; private set; } = null!;

        public static int Main(string[] args)
        {
            try
            {
                // Configure as an Electron app or a web app
                ApplicationConfigurator = args.Any(a => a.Contains("/ELECTRONPORT", StringComparison.OrdinalIgnoreCase))
                    ? new NetPadElectronConfigurator()
                    : new NetPadWebConfigurator();

                CreateHostBuilder(args).Build().Run();
                return 0;
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex);
                Log.Fatal(ex, "Host terminated unexpectedly");
                return 1;
            }
            finally
            {
                Log.CloseAndFlush();
            }
        }

        private static IHostBuilder CreateHostBuilder(string[] args) =>
            Host.CreateDefaultBuilder(args)
                .ConfigureAppConfiguration(config => { config.AddJsonFile("appsettings.Local.json", optional: true); })
                .UseSerilog((ctx, config) =>
                {
                    ConfigureLogging(config, ctx.Configuration);
                })
                .ConfigureWebHostDefaults(webBuilder =>
                {
                    ApplicationConfigurator.ConfigureWebHost(webBuilder, args);
                    webBuilder.UseStartup<Startup>();
                });

        private static void ConfigureLogging(LoggerConfiguration serilogConfig, IConfiguration appConfig)
        {
            Environment.SetEnvironmentVariable("NETPAD_LOG_DIR", Settings.LogFolderPath.Path);

            serilogConfig.ReadFrom.Configuration(appConfig);
        }
    }
}
