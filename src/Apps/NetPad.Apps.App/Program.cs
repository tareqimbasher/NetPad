using System.IO;
using System.Linq;
using System.Net.Http;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using NetPad.Application;
using NetPad.Apps;
using NetPad.Apps.Shells;
using NetPad.Apps.Shells.Electron;
using NetPad.Apps.Shells.Web;
using NetPad.Configuration;
using NetPad.Swagger;
using Serilog;

namespace NetPad;

public static class Program
{
    internal static IShell Shell { get; private set; } = null!;
    private static bool _isSwaggerCodeGenMode;

    public static async Task<int> Main(string[] args)
    {
        if (args.Contains("--swagger"))
        {
            _isSwaggerCodeGenMode = true;
            return await GenerateSwaggerClientCodeAsync(args);
        }

        return RunApp(args);
    }

    private static async Task<int> GenerateSwaggerClientCodeAsync(string[] args)
    {
        try
        {
            Console.WriteLine("Starting host...");
            var host = CreateHostBuilder(args).Build();
            _ = host.RunAsync();

            var hostInfo = host.Services.GetRequiredService<HostInfo>();

            string[] docs =
            [
                "/swagger/NetPad/swagger.json",
                "/swagger/netpad.plugins.omnisharp/swagger.json"
            ];

            var maxLength = docs.Select(x => x.Length).Max();

            using var client = new HttpClient();

            foreach (var doc in docs)
            {
                var url = $"{hostInfo.HostUrl}{doc}";

                Console.Write($"* Generating client code for:   {doc.PadRight(maxLength)}   ... ");
                var response = await client.GetAsync(url);
                var success = response.IsSuccessStatusCode;
                Console.WriteLine(success ? "DONE" : "FAIL");

                if (!success)
                {
                    var content = await response.Content.ReadAsStringAsync();
                    Console.WriteLine(content);
                    return 1;
                }
            }

            await host.StopAsync();
            return 0;
        }
        catch (Exception e)
        {
            Console.WriteLine(e);
            return 1;
        }
    }

    private static int RunApp(string[] args)
    {
        try
        {
            // Select a shell
            Shell = args.Any(a => a.ContainsIgnoreCase("/ELECTRONPORT"))
                ? new ElectronShell()
                : new WebBrowserShell();

            var host = CreateHostBuilder(args).Build();

            Console.WriteLine($"Starting app. Shell: {Shell.GetType().Name}");

            host.Run();
            return 0;
        }
        catch (IOException ioException) when (ioException.Message.ContainsIgnoreCase("address already in use"))
        {
            Console.WriteLine($"Another instance is already running. {ioException.Message}");
            Shell.ShowErrorDialog(
                $"{AppIdentifier.AppName} Already Running",
                $"{AppIdentifier.AppName} is already running. You cannot open multiple instances of {AppIdentifier.AppName}.");
            return 1;
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Host terminated unexpectedly with error:\n{ex}");
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
            .ConfigureAppConfiguration(config => { config.AddJsonFile("appsettings.Local.json", true); })
            .UseSerilog((ctx, config) => { ConfigureLogging(config, ctx.Configuration); })
            .ConfigureWebHostDefaults(webBuilder =>
            {
                if (_isSwaggerCodeGenMode)
                {
                    webBuilder.UseStartup<SwaggerCodeGenerationStartup>();
                }
                else
                {
                    Shell.ConfigureWebHost(webBuilder, args);
                    webBuilder.UseStartup<Startup>();
                }
            });

    private static void ConfigureLogging(LoggerConfiguration serilogConfig, IConfiguration appConfig)
    {
        Environment.SetEnvironmentVariable("NETPAD_LOG_DIR", AppDataProvider.LogDirectoryPath.Path);

        serilogConfig.ReadFrom.Configuration(appConfig);
    }
}
