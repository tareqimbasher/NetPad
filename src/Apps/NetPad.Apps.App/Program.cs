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
        ProgramExitCode result;
        var args = new ProgramArgs(rawArgs);

        if (args.RunMode == RunMode.SwaggerGen)
        {
            result = await GenerateSwaggerClientCodeAsync(args);
        }
        else
        {
            try
            {
                RunApp(args);
                result = ProgramExitCode.Success;
            }
            catch (IOException ioException) when (ioException.Message.ContainsIgnoreCase("address already in use"))
            {
                Console.WriteLine($"Another instance is already running. {ioException.Message}");
                Shell?.ShowErrorDialog(
                    $"{AppIdentifier.AppName} Already Running",
                    $"{AppIdentifier.AppName} is already running. You cannot open multiple instances of {AppIdentifier.AppName}.");
                result =  ProgramExitCode.PortUnavailable;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Host terminated unexpectedly with error:\n{ex}");
                Log.Fatal(ex, "Host terminated unexpectedly");
                result =  ProgramExitCode.UnexpectedError;
            }
            finally
            {
                Log.CloseAndFlush();
            }
        }

        return (int)result;
    }

    private static async Task<ProgramExitCode> GenerateSwaggerClientCodeAsync(ProgramArgs args)
    {
        try
        {
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

    private static void RunApp(ProgramArgs args)
    {
        if (args.ParentPid.HasValue)
        {
            ParentProcessTracker.ExitWhenParentProcessExists(args.ParentPid.Value);
        }

        Shell = args.CreateShell();

        var builder = CreateHostBuilder(args);

        builder.ConfigureServices(s => s.AddSingleton(Shell));

        var host = builder.Build();

        if (args.ParentPid.HasValue)
        {
            ParentProcessTracker.SetThisHost(host);
        }

        host.Run();
    }

    private static IHostBuilder CreateHostBuilder(ProgramArgs args) =>
        Host.CreateDefaultBuilder(args.Raw)
            .ConfigureAppConfiguration(config => { config.AddJsonFile("appsettings.Local.json", true); })
            .UseSerilog((ctx, config) => { ConfigureLogging(config, ctx.Configuration); })
            .ConfigureWebHostDefaults(webBuilder =>
            {
                if (args.RunMode == RunMode.SwaggerGen)
                {
                    webBuilder.UseStartup<SwaggerCodeGenerationStartup>();
                }
                else
                {
                    if (Shell == null)
                    {
                        throw new Exception("Shell has not been initialized");
                    }

                    Shell.ConfigureWebHost(webBuilder, args.Raw);
                    webBuilder.UseStartup<Startup>();
                }
            });

    private static void ConfigureLogging(LoggerConfiguration serilogConfig, IConfiguration appConfig)
    {
        Environment.SetEnvironmentVariable("NETPAD_LOG_DIR", AppDataProvider.LogDirectoryPath.Path);

        serilogConfig.ReadFrom.Configuration(appConfig);
    }
}
