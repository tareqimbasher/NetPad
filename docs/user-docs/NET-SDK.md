NetPad supports .NET 6 and later. You must have at least one of these SDKs installed to use NetPad.

:computer_mouse: To change the selected SDK for a particular script, click the `SDK` dropdown from the toolbar and select the SDK you want to use.

## SDK Installation

NetPad does not install the .NET SDK for you; management of installed SDKs is left to the user.

### Auto-detection
NetPad will attempt to automatically locate where you have installed the .NET SDK. First it will attempt to use the value from one of these environment variables:

* `DOTNET_ROOT`
* `DOTNET_INSTALL_DIR`

If not found, the following locations will be searched (in order):

Windows:
* `{HomeDirectory}\.dotnet`
* `C:\Program Files\dotnet\x64`
* `C:\Program Files\dotnet`

Linux/macOS:
* `{HomeDirectory}/.dotnet`
* `/usr/local/share/dotnet`
* `/usr/share/dotnet`
* `/usr/lib/dotnet`
* `/usr/lib64/dotnet`
* `/opt/dotnet`

If it cannot find your .NET SDK installation in any of those locations it will attempt to execute `dotnet` from your `$PATH` and use it if it's found.

### Manual

You can tell NetPad where your SDK is installed by going to <kbd><kbd>Settings</kbd> > <kbd>General</kbd> > <kbd>Folders</kbd></kbd> and adding the full path to your .NET SDK installation directory in the <kbd>.NET SDK</kbd> setting.