NetPad uses the Monaco editor, the same editor that powers Visual Studio Code. Monaco can be configured by setting options in <kbd><kbd>Settings</kbd> > <kbd>Editor</kbd></kbd>. The options editor will change as you edit the configuration, giving you a live preview of how your main editor will look and behave when you apply those options.

For a full list of available options see: https://microsoft.github.io/monaco-editor/typedoc/interfaces/editor.IEditorOptions.html

## Language Services

C# language services are powered by OmniSharp, a cross-platform and open-source language service for .NET that provides IntelliSense, references, code diagnostics, hints and more.

OmniSharp is known to "freak out" sometimes. If that happens and you stop getting IntelliSense suggestions, restart the OmniSharp server from the Command Pallete: <kbd><kbd>Command Pallete (F1)</kbd> > <kbd>Developer: Restart OmniSharp Server</kbd></kbd>. Wait a couple seconds for it to restart, it should start behaving after that.

If you still experience issues, try restarting NetPad entirely, or open an issue on GitHub.