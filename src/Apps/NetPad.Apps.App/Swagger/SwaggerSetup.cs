using System.Collections.Generic;
using System.IO;
using System.Linq;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.DependencyInjection;
using NetPad.Apps.Plugins;
using NJsonSchema.CodeGeneration.TypeScript;
using NSwag;
using NSwag.CodeGeneration.TypeScript;

namespace NetPad.Swagger;

internal static class SwaggerSetup
{
    public static void AddSwagger(IServiceCollection services, IWebHostEnvironment webHostEnvironment, IEnumerable<PluginRegistration> pluginRegistrations)
    {
        services.AddSwaggerDocument(config =>
        {
            config.Title = "NetPad HTTP Interface";
            config.DocumentName = "NetPad";
            config.OperationProcessors.Insert(0, new ExcludeControllersInAssemblies(pluginRegistrations.Select(p => p.Assembly).ToArray()));

            config.PostProcess = GenerateTypeScriptClientCode(
                Path.Combine(webHostEnvironment.ContentRootPath, "App", "src", "core", "@application", "api.ts"));
        });

        foreach (var pluginRegistration in pluginRegistrations)
        {
            services.AddSwaggerDocument(config =>
            {
                config.Title = $"Plugin - {pluginRegistration.Plugin.Name}";
                config.DocumentName = pluginRegistration.Plugin.Id;
                config.OperationProcessors.Insert(0, new IncludeControllersInAssemblies(pluginRegistration.Assembly));

                string pluginDirName = pluginRegistration.Plugin.Name.Replace(" ", "-");

                foreach (var invalidChar in Path.GetInvalidFileNameChars().Union(Path.GetInvalidPathChars()).Distinct())
                {
                    if (pluginDirName.Contains(invalidChar))
                        pluginDirName = pluginDirName.Replace(invalidChar, '-');
                }

                pluginDirName = pluginDirName.ToLowerInvariant();

                config.PostProcess = GenerateTypeScriptClientCode(
                    Path.Combine(webHostEnvironment.ContentRootPath, "App", "src", "core", "@plugins", pluginDirName, "api.ts"));
            });
        }

        // Interesting options to review:
        // - config.GenerateEnumMappingDescription
    }

    private static Action<OpenApiDocument> GenerateTypeScriptClientCode(string generatedCodeFilePath)
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
                ClientBaseClass = "ApiClientBase",
                TypeScriptGeneratorSettings =
                {
                    TypeScriptVersion = 4.4m,
                    EnumStyle = TypeScriptEnumStyle.StringLiteral,
                    GenerateCloneMethod = true,
                    MarkOptionalProperties = true
                }
            };

            var generator = new TypeScriptClientGenerator(document, settings);

            var code = generator.GenerateFile();

            code = TypeScriptClientCodeTransform.MakeEdits(code);

            File.WriteAllText(generatedCodeFilePath, code);
        };
    }
}
