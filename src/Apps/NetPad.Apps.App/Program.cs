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
using NetPad.Configuration;
using NetPad.Swagger;
using Serilog;

namespace NetPad;

public static class Program
{
    internal static IShell? Shell;

    public static async Task<int> Main(string[] rawArgs)
    {
        var args = new ProgramArgs(rawArgs);

        if (args.RunMode == RunMode.SwaggerGen)
        {
            return (int)await GenerateSwaggerClientCodeAsync(args);
        }

        ProgramExitCode result;

        try
        {
            RunApp(args);
            result = ProgramExitCode.Success;
        }
        catch (IOException ioException) when (ioException.Message.ContainsIgnoreCase("address already in use"))
        {
            Console.WriteLine($"Another instance is already running: {ioException.Message}");
            Log.Fatal(ioException, "Another instance is already running");
            Shell?.ShowErrorDialog(
                $"{AppIdentifier.AppName} Already Running",
                $"{AppIdentifier.AppName} is already running. You cannot open multiple instances of {AppIdentifier.AppName}.");
            result = ProgramExitCode.PortUnavailable;
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Host terminated unexpectedly with error:\n{ex}");
            Log.Fatal(ex, "Host terminated unexpectedly");
            result = ProgramExitCode.UnexpectedError;
        }
        finally
        {
            Log.CloseAndFlush();
        }

        return (int)result;
    }

    private static void RunApp(ProgramArgs args)
    {
        if (args.ParentPid.HasValue)
        {
            ParentProcessTracker.ExitWhenParentProcessExists(args.ParentPid.Value);
        }

        Shell = args.CreateShell();

        var host = CreateHostBuilder<Startup>(args).Build();

        if (args.ParentPid.HasValue)
        {
            ParentProcessTracker.SetThisHost(host);
        }

        host.Run();
    }

    /// <summary>
    /// Starts a web host and calls the swagger endpoint which will in turn generate client code and write it to disk.
    /// </summary>
    private static async Task<ProgramExitCode> GenerateSwaggerClientCodeAsync(ProgramArgs args)
    {
        try
        {
            var host = CreateHostBuilder<SwaggerCodeGenerationStartup>(args).Build();
            _ = host.RunAsync();

            var hostInfo = host.Services.GetRequiredService<HostInfo>();

            string[] docs =
            [
                "swagger/NetPad/swagger.json",
                "swagger/netpad.plugins.omnisharp/swagger.json"
            ];

            var maxLength = docs.Select(x => x.Length).Max();

            using var client = new HttpClient();

            foreach (var doc in docs)
            {
                var url = $"{hostInfo.HostUrl}/{doc}";

                Console.Write($"* Generating client code for:   {doc.PadRight(maxLength)}   ... ");
                var response = await client.GetAsync(url);
                var success = response.IsSuccessStatusCode;
                Console.WriteLine(success ? "DONE" : "FAIL");
                Console.WriteLine("Client code written to disk.");

                if (!success)
                {
                    var content = await response.Content.ReadAsStringAsync();
                    Console.WriteLine(content);
                    return ProgramExitCode.SwaggerGenError;
                }
            }

            await host.StopAsync();
            return ProgramExitCode.Success;
        }
        catch (Exception e)
        {
            Console.WriteLine(e);
            return ProgramExitCode.SwaggerGenError;
        }
    }

    private static IHostBuilder CreateHostBuilder<TStartup>(ProgramArgs args) where TStartup : class =>
        Host.CreateDefaultBuilder(args.Raw)
            .ConfigureAppConfiguration(config =>
            {
                if (args.ShellType == ShellType.Electron)
                {
                    config.AddJsonFile("appsettings.Electron.json", false);
                }
                else if (args.ShellType == ShellType.Tauri)
                {
                    config.AddJsonFile("appsettings.Tauri.json", false);
                }

                config.AddJsonFile("appsettings.Local.json", true);
            })
            .UseSerilog((ctx, config) => { ConfigureLogging(config, ctx.Configuration); })
            .ConfigureWebHostDefaults(webBuilder =>
            {
                webBuilder.UseStartup<TStartup>();

                if (args.RunMode == RunMode.Normal)
                {
                    if (Shell == null)
                    {
                        throw new Exception("Shell has not been initialized");
                    }

                    Shell.ConfigureWebHost(webBuilder, args.Raw);
                }
            });

    private static void ConfigureLogging(LoggerConfiguration serilogConfig, IConfiguration appConfig)
    {
        Environment.SetEnvironmentVariable("NETPAD_LOG_DIR", AppDataProvider.LogDirectoryPath.Path);
        serilogConfig.ReadFrom.Configuration(appConfig);
    }
}
