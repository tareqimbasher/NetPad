# .NET SDK

NetPad is a great way to experiment with new versions of .NET and try out new features. Any .NET SDKs installed on your
machine are automatically detected. You must have at least one .NET SDK **v6 or later** installed. If you have multiple
SDK versions installed, you can change the SDK used on a per-script basis.

:computer_mouse: To change the SDK for a script, click the `SDK` dropdown from the toolbar and select the version you
want to use.

!> NetPad does not install a .NET SDK for you; users are expected to install it themselves.

## Auto-detection

NetPad will try to locate where you installed the .NET SDK. First it will attempt to use the value from one of these
environment variables:

* `DOTNET_ROOT`
* `DOTNET_INSTALL_DIR`

If those variables don't exist, or their values don't resolve to a valid .NET installation, the following locations will
be searched (**in order**) until a valid .NET installation is detected:

**Windows:**

* `{HomeDirectory}\.dotnet`
* `C:\Program Files\dotnet\x64`
* `C:\Program Files\dotnet`

**Linux/BSD/macOS:**

* `{HomeDirectory}/.dotnet`
* `/usr/local/share/dotnet`
* `/usr/share/dotnet`
* `/usr/lib/dotnet`
* `/usr/lib64/dotnet`
* `/opt/dotnet`

If it cannot find your .NET SDK installation in any of those locations it will attempt to execute `dotnet` from your
`$PATH` and use it if it is found.

## Manual

You can tell NetPad where you want it to load the SDK from by going to <kbd><kbd>Settings</kbd> > <kbd>
General</kbd> > <kbd>
Folders</kbd></kbd> and adding the full path to your .NET SDK installation directory in the <kbd>.NET SDK</kbd> setting.

## Issues

If NetPad is complaining that it cannot find your .NET SDK installation:

1. Set `DOTNET_ROOT` or `DOTNET_INSTALL_DIR` environment variables to the directory where you have the .NET SDK
   installed.
2. Or, make sure you have the .NET SDK installed in one of the locations outlined in
   the [Auto-detection](#auto-detection) section above.
3. Try setting the location manually in Settings.
4. If all else fails, see: [How to Get Help](/wiki/Troubleshooting#how-to-get-help).
