# Editor

NetPad uses the [Monaco editor](https://github.com/microsoft/monaco-editor), the same editor that powers Visual Studio
Code.

## Configuration

Monaco can be configured by setting options in <kbd><kbd>Settings</kbd> > <kbd>Editor</kbd></kbd>. You will see a live
preview as you edit the configuration, making it easier to see how your main editor will behave when you apply your
changes.

For a full list of available options
see: https://microsoft.github.io/monaco-editor/typedoc/interfaces/editor.IEditorOptions.html

## Language Services

C# language services are powered by [OmniSharp](https://github.com/OmniSharp/omnisharp-roslyn), a cross-platform and
open-source language service for .NET that provides IntelliSense, references, code diagnostics, hints and more. When you
start NetPad for the first time, it will download OmniSharp automatically for you.

### OmniSharp Issues

OmniSharp is known to "freak out" sometimes. If that happens, and you stop getting IntelliSense suggestions, restart the
OmniSharp server from the editor Command Palette: <kbd><kbd>Command Palette (F1)</kbd> > <kbd>Developer: Restart
OmniSharp Server</kbd></kbd>. Wait a couple seconds for it to restart, it should start behaving after that.

If you still experience issues, try restarting NetPad entirely,
or see [How to Get Help](/wiki/Troubleshooting#how-to-get-help).

## Styling

See [Styling the Editor](/wiki/Styling?id=styling-the-editor) for how to style the editor in NetPad.