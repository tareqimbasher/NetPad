using System;
using System.Collections.Generic;
using System.Linq;

namespace NetPad.Swagger;

internal static class TypeScriptClientCodeTransform
{
    public static string MakeEdits(string code)
    {
        var lines = code
            // Replace fetch 'http' param type with Aurelia's IHttpClient interface
            .Replace(
                "private http: { fetch(url: RequestInfo, init?: RequestInit): Promise<Response> };",
                "private http: IHttpClient;")
            .Replace(
                "http?: { fetch(url: RequestInfo, init?: RequestInit): Promise<Response> }",
                "@IHttpClient http?: IHttpClient")
            .Replace(
                "return this.http.fetch(url_, options_)",
                "return this.makeFetchCall(() => this.http.fetch(url_, options_))")

            // Convert to lines
            .Split("\n")

            // Remove all imports and @inject decorators
            .Where(l => !l.StartsWith("import") && !l.StartsWith("@inject"))
            .ToList();

        // We don't want the generated file to be linted
        lines.Insert(0, "// @ts-nocheck");

        // Add the imports we want
        lines.InsertRange(9,new[]
        {
            "import {IHttpClient} from \"aurelia\";",
            "import {ApiClientBase} from \"@domain/api-client-base\";",
        });

        // Other transforms
        AddAbortSignalParametersToApiClientInterfaces(lines);

        return string.Join(Environment.NewLine, lines);
    }

    private static void AddAbortSignalParametersToApiClientInterfaces(List<string> lines)
    {
        // NSwag adds AbortSignal parameters to generated api client implementations
        // but not to their interfaces. Here we add the AbortSignal param to the interfaces

        bool insideApiClientInterface = false;
        for (var iLine = 0; iLine < lines.Count; iLine++)
        {
            var line = lines[iLine];
            if (!insideApiClientInterface)
            {
                if (line.StartsWith("export interface ") && line.EndsWith("ApiClient {"))
                {
                    insideApiClientInterface = true;
                }

                continue;
            }

            if (line.Trim() == "}")
            {
                insideApiClientInterface = false;
                continue;
            }

            if (string.IsNullOrWhiteSpace(line)) continue;

            var insertIndex = line.LastIndexOf("):", StringComparison.Ordinal);
            if (insertIndex < 0) continue;

            var text = line[insertIndex - 1] == '('
                ? ""
                // Prepend a comma if there are other args
                : ", ";

            text += "signal?: AbortSignal | undefined";

            lines[iLine] = line.Insert(insertIndex, text);
        }
    }
}
