using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using Microsoft.Extensions.DependencyInjection;
using NetPad.Plugins;
using NJsonSchema.CodeGeneration.TypeScript;
using NSwag;
using NSwag.CodeGeneration.TypeScript;
using NSwag.Generation.Processors;
using NSwag.Generation.Processors.Contexts;

namespace NetPad;

public partial class Startup
{
    private void AddSwagger(IServiceCollection services, IEnumerable<PluginRegistration> pluginRegistrations)
    {
        services.AddSwaggerDocument(config =>
        {
            config.Title = "NetPad";
            config.DocumentName = "NetPad";
            config.OperationProcessors.Insert(0, new ExcludeControllersInAssemblies(pluginRegistrations.Select(p => p.Assembly).ToArray()));

            config.PostProcess = GetPostProcessAction(
                Path.Combine(WebHostEnvironment.ContentRootPath, "App", "src", "core", "@domain", "api.ts"));
        });

        foreach (var pluginRegistration in pluginRegistrations)
        {
            services.AddSwaggerDocument(config =>
            {
                config.Title = $"NetPad Plugin - {pluginRegistration.Plugin.Name}";
                config.DocumentName = config.Title;
                config.OperationProcessors.Insert(0, new IncludeControllersInAssemblies(pluginRegistration.Assembly));

                string pluginDirName = pluginRegistration.Plugin.Name.Replace(" ", "-");

                foreach (var invalidChar in Path.GetInvalidFileNameChars().Union(Path.GetInvalidPathChars()).Distinct())
                {
                    if (pluginDirName.Contains(invalidChar))
                        pluginDirName = pluginDirName.Replace(invalidChar, '-');
                }

                pluginDirName = pluginDirName.ToLowerInvariant();

                config.PostProcess = GetPostProcessAction(
                    Path.Combine(WebHostEnvironment.ContentRootPath, "App", "src", "core", "@plugins", pluginDirName, "api.ts"));
            });
        }

        // Interesting options to review:
        // - config.GenerateEnumMappingDescription
    }

    private Action<OpenApiDocument> GetPostProcessAction(string generatedCodeFilePath)
    {
        return document =>
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
                    TypeScriptVersion = 4.4m,
                    EnumStyle = TypeScriptEnumStyle.StringLiteral,
                    GenerateCloneMethod = true,
                    MarkOptionalProperties = true,
                }
            };

            var generator = new TypeScriptClientGenerator(document, settings);

            var lines = generator.GenerateFile()
                .Replace(
                    "private http: { fetch(url: RequestInfo, init?: RequestInit): Promise<Response> };",
                    "private http: IHttpClient;")
                .Replace(
                    "http?: { fetch(url: RequestInfo, init?: RequestInit): Promise<Response> }",
                    "@IHttpClient http?: IHttpClient")
                .Split("\n")
                .Where(l => !l.StartsWith("import") && !l.StartsWith("@inject"))
                .ToList();

            lines.Insert(0, "// @ts-nocheck");
            lines.Insert(9, "import {IHttpClient} from \"aurelia\";");

            int ixApiExceptionCode = lines.FindIndex(l => l.Contains("static isApiException(obj: any): obj is ApiException {"));
            if (ixApiExceptionCode > 0)
            {
                // Insert our code before marker
                // lines.Insert(ixApiExceptionCode, "");
            }

            File.WriteAllText(generatedCodeFilePath, string.Join(Environment.NewLine, lines));
        };
    }

    private class IncludeControllersInAssemblies : IOperationProcessor
    {
        private readonly Assembly[] _assemblies;

        public IncludeControllersInAssemblies(params Assembly[] assemblies)
        {
            _assemblies = assemblies;
        }

        public bool Process(OperationProcessorContext context)
        {
            return _assemblies.Contains(context.ControllerType.Assembly);
        }
    }

    private class ExcludeControllersInAssemblies : IOperationProcessor
    {
        private readonly Assembly[] _assemblies;

        public ExcludeControllersInAssemblies(params Assembly[] assemblies)
        {
            _assemblies = assemblies;
        }

        public bool Process(OperationProcessorContext context)
        {
            return !_assemblies.Contains(context.ControllerType.Assembly);
        }
    }
}
