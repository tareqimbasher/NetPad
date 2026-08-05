# Script Execution Pipeline

This document describes the end-to-end flow of what happens when a user runs a script in NetPad — from the moment
they click "Run" to when output appears in the Output pane.

## Overview

Script execution involves coordination across multiple projects and processes:

1. A **command** is dispatched to start execution.
2. **Dependencies** are resolved (assemblies, NuGet packages, data connection resources).
3. User code is **parsed and compiled** into a .NET assembly using Roslyn.
4. The compiled assembly and its dependencies are **deployed** to a temporary directory.
5. A separate process loads and **runs** the assembly.
6. **Output** flows back to the caller via stdio and is presented to the user.

Steps 1–3 are shared across all execution models. Steps 4–6 differ depending on which execution model is in use.

## Command Dispatch

The SPA calls a REST endpoint which dispatches `RunScriptCommand` via MediatR. The handler retrieves the
`ScriptEnvironment` for the given script and calls `RunAsync()`.

**Key file:** `Apps/NetPad.Apps.Common/CQs/RunScriptCommand.cs`

`ScriptEnvironment` is the managed execution context for a script. It tracks the script's status (Ready, Running,
Stopping, Error), holds references to input readers and output writers, and delegates actual execution to an
`IScriptRunner` implementation.

**Key file:** `Core/NetPad.Runtime/Scripts/ScriptEnvironment.cs`

## Execution Models

NetPad has two execution models, selected at startup via dependency injection. Both implement `IScriptRunner` but
differ in process lifetime, communication, and capabilities.

| Aspect                 | ClientServer                                    | External                                        |
|------------------------|-------------------------------------------------|-------------------------------------------------|
| **Used by**            | Desktop & web app                               | CLI (`npad`)                                    |
| **Process lifetime**   | Long-lived, reused across runs                  | Short-lived, one process per run                |
| **Communication**      | Structured message protocol over stdio           | stdout/stderr with JSON messages                |
| **Memory cache**       | Persistent across runs                           | None (process exits after each run)             |
| **User input**         | Interactive (prompts via IPC)                    | Reads from stdin                                |
| **Output formats**     | HTML (for Output pane)                           | Console, text, HTML, HTML doc, JSON             |
| **DI registration**    | `AddClientServerExecutionModel()`               | `AddExternalExecutionModel()`                   |

**Key file:** `Core/NetPad.Runtime/ExecutionModel/DependencyInjection.cs`

Each model registers its own `IScriptRunnerFactory` and `ICodeParser`:

- **ClientServer:** `ClientServerScriptRunnerFactory` + `ClientServerCSharpCodeParser`
- **External:** `ExternalScriptRunnerFactory` + `ExternalRunnerCSharpCodeParser`

## Dependency Resolution

Before compilation, all external dependencies must be gathered. `ScriptDependencyResolver` collects:

- **Assembly references** added by the user (NuGet packages, DLL references).
- **Data connection resources** — assemblies and generated code from linked database connections.
- **NetPad runtime assemblies** — the runtime library, O2Html serializer, and other user-visible assemblies.

Each dependency is classified by who needs it:

| Dependant        | Description                                   |
|------------------|-----------------------------------------------|
| `Script`         | Only needed by the compiled user script        |
| `ScriptHost`     | Only needed by the host process                |
| `Shared`         | Needed by both                                 |

**Key file:** `Core/NetPad.Runtime/Compilation/Scripts/Dependencies/ScriptDependencyResolver.cs`

## Parsing & Compilation

`ScriptCompiler` orchestrates turning user code into a runnable assembly.

**Key file:** `Core/NetPad.Runtime/Compilation/Scripts/ScriptCompiler.cs`

### Code Permutations

The compiler tries multiple permutations of the user's code to support expression-style input:

1. The code as-is.
2. Wrapped with `.Dump();` — so typing `DateTime.Now` auto-dumps the result.
3. With a trailing `;` — to execute bare expressions.

The first permutation that compiles successfully is used.

### Parsing

`ICodeParser` transforms user code into a full runnable program by:

- Wrapping it in bootstrapper code (a `Main` method entry point).
- Adding default `using` statements.
- Injecting script metadata (`SCRIPT_ID`, `SCRIPT_NAME`, `SCRIPT_LOCATION`).

Each execution model has its own `ICodeParser` implementation since the bootstrapper code differs (the ClientServer
model injects minimal bootstrapping while the External model embeds a full `Program.cs` with process setup, output
format handling, and parent-exit detection).

### Compilation

`CSharpCodeCompiler` uses Roslyn (`Microsoft.CodeAnalysis`) to compile the parsed code. It:

- Creates a `CSharpCompilation` with the syntax tree, framework assemblies, and user references.
- Runs any built-in source generators.
- Emits IL bytecode to a `MemoryStream`.
- Returns a `CompilationResult` containing the assembly bytes and diagnostics.

If compilation fails, diagnostics are filtered to remove noise from bootstrapper code and sent back as error output.

**Key file:** `Core/NetPad.Runtime/Compilation/CSharp/CSharpCodeCompiler.cs`

### Caching

To avoid redundant work on repeated runs, a **fingerprint hash** is derived from:

- Script code
- Script configuration (target framework, optimization level, ASP.NET flag)
- Referenced assemblies
- Data connection identity

If the fingerprint matches a previous run, both compilation and deployment are skipped entirely, and the cached
assembly is reused. Both execution models use this caching strategy.

---

## ClientServer Model

Used by the desktop and web apps. This is the "Client" half of a client-server pair:

- **Client** = the main NetPad process (`ClientServerScriptRunner`)
- **Server** = a separate long-running `script-host` process

The client manages the full lifecycle: compiling code, deploying artifacts, spawning the script-host process, and
communicating with it over stdio.

**Key file:** `Core/NetPad.Runtime/ExecutionModel/ClientServer/ClientServerScriptRunner.cs`

```
 ┌──────────┐   REST    ┌──────────────┐  RunAsync   ┌───────────────────────────┐
 │   SPA    │ ────────► │  Controller  │ ──────────► │    ScriptEnvironment      │
 └──────────┘           └──────────────┘             └─────────────┬─────────────┘
                                                                   │
                                                                   ▼
                                                     ┌───────────────────────────┐
                                                     │ ClientServerScriptRunner  │
                                                     │  1. Resolve dependencies  │
                                                     │  2. Compile              │
                                                     │  3. Deploy to filesystem │
                                                     │  4. Send run message     │
                                                     └─────────────┬─────────────┘
                                                           stdio   │
                                                                   ▼
                                                     ┌───────────────────────────┐
                                                     │   script-host process     │
                                                     │  1. Load assembly        │
                                                     │  2. Execute entry point  │
                                                     │  3. Stream output back   │
                                                     └───────────────────────────┘
```

### Run Flow

When `RunScriptAsync()` is called, it:

1. Acquires a lock to prevent concurrent runs of the same script.
2. Checks whether the existing script-host process needs to be restarted (e.g. target framework changed).
3. Sets up the run environment (dependency resolution, compilation, deployment).
4. Sends a `RunScriptMessage` to the script-host via stdio IPC.
5. Waits for a `ScriptRunCompleteMessage` response.

### Deployment

Artifacts are deployed to a temporary directory structure:

```
{temp root}/                         # Per-script working directory
├── script-host/                     # Script-host executable
├── shared-deps/                     # Assemblies needed by both script-host and script
└── script/{runId}/                  # Per-run directory
    ├── {scriptName}__.dll           # Compiled script assembly
    ├── {scriptName}__.runtimeconfig.json
    └── scriptconfig.json            # Output serialization settings
```

A `.runtimeconfig.json` is generated with the target framework version and assembly probing paths so the .NET
runtime can locate all dependencies.

**Key file:** `Core/NetPad.Runtime/ExecutionModel/ClientServer/ClientServerScriptRunner.Setup.cs`

### Script-Host Process

The script-host is a separate .NET executable (`NetPad.Apps.ScriptHost`). One instance is spawned per script and
stays alive across multiple runs for fast consecutive execution.

**Key files:**
- `Apps/NetPad.Apps.ScriptHost/Program.cs` — entry point
- `Apps/NetPad.Apps.ScriptHost/ScriptRunner.cs` — execution logic

#### Startup

1. Receives the parent process ID as a command-line argument.
2. Sets up parent-exit detection (terminates itself if the parent dies).
3. Initializes a `StdioIpcGateway` for bidirectional messaging.
4. Sends a `ScriptHostReadyMessage` to signal it's ready.
5. Enters an infinite wait loop, handling messages as they arrive.

#### Script Execution

When a `RunScriptMessage` arrives, `ScriptRunner.Run()`:

1. Loads script-host dependency assemblies into the default `AssemblyLoadContext`.
2. Creates an `UnloadableAssemblyLoadContext` — an isolated context for the user's script assembly.
3. Loads the compiled script assembly into the isolated context.
4. Invokes the assembly's entry point (`Main` method).
5. On completion (or exception), sends a `ScriptRunCompleteMessage` back to the client.

The isolated `AssemblyLoadContext` is disposed after each run, allowing the runtime to unload the user's assembly and
reclaim memory.

### Output Flow

#### Inside the Script-Host

Before the script runs, `ClientServerDumpSink` redirects `Console.Out` and `Console.In`:

- **Output:** All `Console.Write`/`Dump()` calls are intercepted. Objects are serialized to HTML by
  `ClientServerOutputHtmlWriter` (which uses `O2Html` under the hood), wrapped in a `ScriptOutputMessage`, and sent
  to the client via stdio.
- **Input:** When user code calls `Console.ReadLine()`, a `RequestUserInputMessage` is sent to the client, which
  prompts the user and sends the response back as a `ReceiveUserInputMessage`.

#### Back in the Client

`ClientServerScriptRunner` registers message handlers on the stdio IPC gateway:

| Message                      | Handler behavior                                              |
|------------------------------|---------------------------------------------------------------|
| `ScriptOutputMessage`        | Deserializes output, writes to output writers (then to UI)    |
| `RequestUserInputMessage`    | Prompts external input readers, sends response back           |
| `ScriptRunCompleteMessage`   | Completes the run, updates script status                      |
| `ScriptHostExitedMessage`    | Handles unexpected process termination                        |

If output can't be deserialized (e.g. raw console output from a library), it falls through to `RawOutputHandler`,
which buffers, debounces, and forwards it as `RawScriptOutput`.

**Key file:** `Core/NetPad.Runtime/ExecutionModel/RawOutputHandler.cs`

#### Reaching the UI

Output writers on the `ScriptEnvironment` forward output to the SPA via SignalR, where it is rendered in the
Output pane.

### IPC Message Reference

#### Client → Script-Host

| Message                    | Purpose                                      |
|----------------------------|----------------------------------------------|
| `RunScriptMessage`         | Trigger script execution with paths & config  |
| `ReceiveUserInputMessage`  | Provide user input response                   |
| `DumpMemCacheItemMessage`  | Request dump of a cached item                 |
| `DeleteMemCacheItemMessage`| Remove item from memory cache                 |
| `ClearMemCacheMessage`     | Clear entire memory cache                     |

#### Script-Host → Client

| Message                          | Purpose                                  |
|----------------------------------|------------------------------------------|
| `ScriptHostReadyMessage`         | Signal that script-host is initialized   |
| `ScriptOutputMessage`            | Script output (HTML-serialized)          |
| `RequestUserInputMessage`        | Script is requesting user input          |
| `ScriptRunCompleteMessage`       | Run finished (with result or error)      |
| `ScriptHostExitedMessage`        | Process is exiting                       |
| `MemCacheItemInfoChangedMessage` | Memory cache contents changed            |

---

## External Model

Used by the CLI (`npad`). Designed for standalone, non-interactive script execution where each run is fully
isolated.

**Key files:**
- `Core/NetPad.Runtime/ExecutionModel/External/ExternalScriptRunner.cs` — main runner
- `Core/NetPad.Runtime/ExecutionModel/External/ExternalScriptRunner.Setup.cs` — build & deployment
- `Core/NetPad.Runtime/ExecutionModel/External/ExternalScriptRunner.IO.cs` — I/O handling

```
 ┌──────────┐           ┌───────────────────────────┐
 │   CLI    │ ────────► │    ScriptEnvironment      │
 └──────────┘           └─────────────┬─────────────┘
                                      │
                                      ▼
                        ┌───────────────────────────┐
                        │  ExternalScriptRunner     │
                        │  1. Resolve dependencies  │
                        │  2. Compile              │
                        │  3. Deploy to filesystem │
                        │  4. Spawn process        │
                        └─────────────┬─────────────┘
                              stdout  │
                                      ▼
                        ┌───────────────────────────┐
                        │   Short-lived process     │
                        │  1. Bootstrap & setup    │
                        │  2. Execute script       │
                        │  3. Exit                 │
                        └───────────────────────────┘
```

### Run Flow

When `RunScriptAsync()` is called, it:

1. Sets up the run environment (dependency resolution, compilation, deployment).
2. Spawns a **new process** for the compiled script assembly using `dotnet exec`.
3. Pipes stdout/stderr back to the caller.
4. The process exits when the script completes.

Unlike the ClientServer model, there is no persistent server — each run starts and ends a fresh process.

### Deployment

The deployment structure is similar to ClientServer but self-contained. All dependencies, the compiled assembly,
and a `.runtimeconfig.json` are written to a cache directory. The `DeploymentCache` tracks deployments by
fingerprint hash to reuse previous builds.

**Key file:** `Core/NetPad.Runtime/ExecutionModel/External/ExternalScriptRunner.Setup.cs`

### Bootstrapper

The External model's `ICodeParser` (`ExternalRunnerCSharpCodeParser`) embeds a more substantial bootstrapper than
the ClientServer model. The embedded `Program.cs` handles:

- Parent process exit detection (terminates if the parent dies).
- Output format selection based on CLI arguments (`-console`, `-text`, `-json`, `-html`, `-html-msg`).
- Initializing the `ExternalProcessDumpSink` for formatted output.
- ASP.NET-specific environment variable setup.

**Key file:** `Core/NetPad.Runtime/ExecutionModel/External/EmbeddedCode/Program.cs`

### Output

Output is written directly to stdout. The format depends on CLI flags:

| Flag          | Output behavior                                             |
|---------------|-------------------------------------------------------------|
| `-console`    | Plain text to console (default)                             |
| `-text`       | Raw text output                                             |
| `-html`       | HTML-serialized objects                                     |
| `-html-msg`   | HTML wrapped in JSON messages (same format as ClientServer) |
| `-json`       | NDJSON (newline-delimited JSON)                             |

The CLI reads stdout and presents it using `Spectre.Console` for rich terminal rendering, or passes it through
directly depending on the chosen format.

### Limitations vs ClientServer

- **No memory cache** — `DumpMemCacheItem`, `DeleteMemCacheItem`, and `ClearMemCacheItems` are no-ops.
- **No interactive input protocol** — user input is read directly from stdin rather than via IPC messages.
- **No process reuse** — every run pays the full .NET host startup cost.

These trade-offs are appropriate for CLI usage where simplicity and isolation matter more than interactivity.

### Configuration Options

`ExternalScriptRunnerOptions` provides CLI-specific controls:

| Option           | Purpose                                                    |
|------------------|------------------------------------------------------------|
| `NoCache`        | Skip the deployment cache, always rebuild                  |
| `ForceRebuild`   | Force recompilation even if fingerprint matches            |
| `ProcessCliArgs` | Additional arguments passed to the spawned process         |
| `RedirectIo`     | Whether to capture stdout/stderr (vs letting it pass through) |

---

## Error Handling

Error handling is shared across both models:

**Compilation errors** are extracted from Roslyn diagnostics, adjusted to remove line-number offsets introduced by
the bootstrapper code, and sent as `ErrorScriptOutput`.

**Runtime exceptions** are caught in the execution process. The stack trace line numbers are adjusted relative to
the user's code (subtracting the bootstrapper offset). In the ClientServer model, the error is serialized in
`ScriptRunCompleteMessage` and forwarded to the client. In the External model, errors are written to stderr.

**Process failures** — in the ClientServer model, if the script-host exits unexpectedly, the
`ScriptHostExitedMessage` handler marks the run as failed. In the External model, a non-zero exit code signals
failure.
