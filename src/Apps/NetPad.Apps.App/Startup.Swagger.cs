using System;
using System.IO;
using System.Linq;
using Microsoft.Extensions.DependencyInjection;
using NJsonSchema.CodeGeneration.TypeScript;
using NSwag.CodeGeneration.TypeScript;

namespace NetPad;

public partial class Startup
{
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
