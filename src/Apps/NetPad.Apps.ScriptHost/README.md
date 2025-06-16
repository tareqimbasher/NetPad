# `script-host`

The `script-host` is a long-running executable launched by NetPad as part of its `ClientServer`
execution model. The `script-host` is the "Server". It is responsible for:

1. Executing compiled script assemblies.
2. Sending script output to NetPad.
3. Retaining state and cache data between script runs.

It is **not** responsible for:

1. Parsing user code.
2. Compiling and emitting script assemblies.
3. Interfacing with the UI

A dedicated `script-host` process instance is started for each script the user runs.
Multiple runs of the same script will reuse the same `script-host`.

Two-way communication between the `script-host` and NetPad occurs by sending messages
(which are known by both applications) via redirected **standard input/output streams** (stdio).
