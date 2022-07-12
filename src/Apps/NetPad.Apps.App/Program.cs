using System;
using System.Linq;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Console;
using NetPad.Electron;
using NetPad.Web;

namespace NetPad
{
    public static class Program
    {
        internal static IApplicationConfigurator ApplicationConfigurator { get; private set; } = null!;

        public static void Main(string[] args)
        {
            // Configure as an Electron app or a web app
            ApplicationConfigurator = args.Any(a => a.Contains("/ELECTRONPORT", StringComparison.OrdinalIgnoreCase))
                ? new NetPadElectronConfigurator()
                : new NetPadWebConfigurator();

            CreateHostBuilder(args).Build().Run();
        }

        public static void ConfigureLogging(ILoggingBuilder builder)
        {
            builder.AddSimpleConsole(c => c.ColorBehavior = LoggerColorBehavior.Enabled);
        }

        private static IHostBuilder CreateHostBuilder(string[] args) =>
            Host.CreateDefaultBuilder(args)
                .ConfigureAppConfiguration(config => { config.AddJsonFile("appsettings.Local.json", optional: true); })
                .ConfigureLogging(ConfigureLogging)
                .ConfigureWebHostDefaults(webBuilder =>
                {
                    ApplicationConfigurator.ConfigureWebHost(webBuilder, args);
                    webBuilder.UseStartup<Startup>();
                });
    }
}
