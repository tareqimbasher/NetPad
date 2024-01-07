# OmniSharp.NET

OmniSharp.NET simplifies interaction with OmniSharp by offering a user-friendly API for .NET applications. 
This library streamlines communication with OmniSharp's STDIO interface, providing a seamless experience
for developers working in the .NET ecosystem. With plans for future expansion to support HTTP interfacing, 
OmniSharp.NET aims to be a comprehensive tool for integrating OmniSharp functionality into your applications. 

This library powers [NetPad](https://github.com/tareqimbasher/NetPad).

TODO before publishing a nuget package:
- Add instructions and usage examples

### Example Usage

```csharp
var builder = WebApplication.CreateBuilder();

builder.Services.AddOmniSharpServer();

var app = builder.Build();

// ...

var factory = app.Services.GetRequiredService<IOmniSharpServerFactory>();

IOmniSharpStdioServer omniSharpServer = factory.CreateStdioServerFromNewProcess(
    string executablePath,
    string projectPath,
    string? additionalArgs,
    string? dotNetSdkRootDirectoryPath);

await omniSharpServer.StartAsync();

var response = await omniSharpServer.SendAsync<[RESPONSE]>([REQUEST], cancellationToken);
```
