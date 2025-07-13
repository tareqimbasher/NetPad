# The `Util` class

NetPad provides a handy static `Util` class to enhance your scripting experience with built-in helpers for 
dumping data, caching, environment access, and more.

## Environment

Access environment and script metadata:

```csharp
Util.Script.Dump();
Util.Environment.Dump();
Util.Stopwatch.Elapsed.Dump();
```

## Caching
Store and retrieve values with `Util.Cache`. Useful for expensive computations or repeated lookups:

```csharp
var albums = Util.Cache.GetOrAdd("albums", () => Albums.ToArray());
```

> :bulb: Explore other `Util.Cache` methods via IntelliSense—they're well-documented and easy to use.

## Open Files, Folders, and URLs

Open files, folders and URLs using their default application handlers right from your script:

```csharp
Util.OpenFile("/path/img.png");
Util.OpenDirectory("/path/dir");
Util.OpenUrl("https://github.com");
```

## Dumping Output

Dump an object, or value, for debugging and visualization:

```csharp
Util.Dump<T>(
    T? o, 
    string? title = null, 
    string? css = null, 
    string? code = null, 
    int? clear = null);
    
Util.Dump<T>(T? o, DumpOptions options);
```

Dump formatted code snippets:

```csharp
Util.DumpCode("<h1>Hello</h1>");
```

Render raw HTML:

```csharp
Util.RawHtml("<h1>Heading 1</h1>");
Util.RawHtml(XElement.Parse("<h1>Heading 1</h1>"));
```

## Host Lifecycle

Control how the script host behaves:

```csharp
Util.RestartHostOnEveryRun = true; // Disables persisting state across runs. Default: false
Util.Terminate();                  // Terminates the running script and host process
```