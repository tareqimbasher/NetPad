# .NET SDK

NetPad is a great way to experiment with new versions of .NET and try out new features. .NET SDKs installed on your
machine are automatically detected from all known installation locations. You must have at least one .NET SDK **v6 or
later** installed. If you have multiple SDK versions installed (even across different installation directories), you can
change the SDK used on a per-script basis.

:computer_mouse: To change the SDK for a script, click the `SDK` dropdown from the toolbar and select the version you
want to use.

!> NetPad does not install a .NET SDK for you; users are expected to install it themselves.

## Auto-detection

NetPad searches all known .NET installation locations and aggregates SDKs from every valid installation it finds.
When running a script, NetPad automatically uses the installation that has the matching SDK for the script's target
framework. The locations are checked in the following priority order:

1. **User setting** (if set) — the path configured in <kbd>Settings > General > Folders > .NET SDK</kbd> (see
   [Manual](#manual) below)
2. **Environment variables** — `DOTNET_ROOT` and `DOTNET_INSTALL_DIR`
3. **User-level installation** — `{HomeDirectory}/.dotnet`
4. **System-level installations:**

   **Windows:**
    * `C:\Program Files\dotnet\x64`
    * `C:\Program Files\dotnet`

   **Linux/BSD/macOS:**
    * `/usr/local/share/dotnet`
    * `/usr/share/dotnet`
    * `/usr/lib/dotnet`
    * `/usr/lib64/dotnet`
    * `/opt/dotnet`

5. **PATH fallback** — if none of the above locations are valid, NetPad will attempt to execute `dotnet` from your
   `$PATH` and use it if found.

## Manual

You can tell NetPad which .NET installation to prioritize by going to <kbd><kbd>Settings</kbd> > <kbd>General</kbd> >
<kbd>Folders</kbd></kbd> and setting the full path to your .NET SDK installation directory in the <kbd>.NET SDK</kbd>
setting. This path will be checked first, but SDKs from other installations will still be detected and available.

## Issues

If NetPad is complaining that it cannot find your .NET SDK installation:

1. Open the **App Dependencies** dialog (<kbd>Tools > App Dependency Check</kbd>) to see which SDKs and
   installations NetPad has detected.
2. Make sure you have at least one .NET SDK **v6 or later** installed in one of the locations listed in
   the [Auto-detection](#auto-detection) section above.
3. Try setting the installation path manually in Settings (see [Manual](#manual) above).
4. Set the `DOTNET_ROOT` or `DOTNET_INSTALL_DIR` environment variable to the directory where you have the .NET SDK
   installed, then restart NetPad.
5. If all else fails, see: [How to Get Help](/wiki/Troubleshooting#how-to-get-help).
