# `script-host`

The `script-host` is a long-running executable launched by NetPad as part of its `ClientServer`
execution model. The `script-host` is the "Server" and NetPad is the client. It is responsible for:

1. Executing compiled script assemblies.
2. Sending script output to NetPad.
3. Retaining state and cache data between script runs.

It is **NOT** responsible for:

1. Parsing user code.
2. Compiling and emitting script assemblies.
3. Interfacing with the UI directly.

A dedicated `script-host` process instance is started for each script the user runs.
Multiple runs of the same script will reuse the same `script-host`. Two different scripts
will have to two different `script-host` processes.

Two-way inter-process communication (IPC) between the `script-host` and NetPad occurs by sending
messages (which are known by both applications) via redirected **standard input/output streams**
(stdio).

## Lifetime

The `script-host` process stops/terminates when the parent process (NetPad) terminates it, or when
the parent process itself is terminated.

NetPad might choose to terminate this process (and possibly respawn it) when necessary, mostly for
reasons related to user changes to the script that require a new `script-host` to be spawned.
Examples:

1. User changed the script's target .NET SDK.
2. User selected a different data connection.
3. User stopped the script from the UI before it was finished (the little red button).
4. User stopped all runners (script-hosts) from the UI Tools menu. 
5. User set `Util.RestartHostOnEveryRun = true;` in their script code.
6. User terminated the `script-host` process from their code: `Environment.Exit(0)`
