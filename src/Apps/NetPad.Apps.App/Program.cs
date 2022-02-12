using System;
using System.Linq;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Hosting;
using NetPad.Electron;
using NetPad.Web;

namespace NetPad
{
    public class Program
    {
        public static IApplicationConfigurator ApplicationConfigurator { get; private set; } = null!;

        public static void Main(string[] args)
        {
            ApplicationConfigurator = args.Any(a => a.Contains("/ELECTRONPORT", StringComparison.OrdinalIgnoreCase))
                ? new NetPadElectronConfigurator()
                : new NetPadWebConfigurator();

            CreateHostBuilder(args).Build().Run();
        }

        public static IHostBuilder CreateHostBuilder(string[] args) =>
            Host.CreateDefaultBuilder(args)
                .ConfigureAppConfiguration((hostingContext, config) => { config.AddJsonFile("appsettings.Local.json", optional: true); })
                .ConfigureWebHostDefaults(webBuilder =>
                {
                    ApplicationConfigurator.ConfigureWebHost(webBuilder, args);
                    webBuilder.UseStartup<Startup>();
                });
    }
}
