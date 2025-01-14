# `script-host`

The `script-host` is a long-running executable launched by NetPad as part of its `ClientServer`
execution model. It is responsible for executing compiled script assemblies while
maintaining state and data between script runs.

For each script executed in NetPad, a dedicated `script-host` process instance is created and kept
alive until the script is closed or the `script-host` needs to be recycled.

Two-way communication between the `script-host` and NetPad is achieved through a predefined set
of messages shared by both applications. These messages are exchanged via redirected **standard
input/output streams** (stdio).
