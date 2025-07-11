These are common issues that some users will run into and how to resolve them.

## NetPad cannot find my .NET SDK installation

See the [.NET SDK](https://github.com/tareqimbasher/NetPad/wiki/.NET-SDK) page. If you are still having trouble please open an Issue.

## NetPad is damaged and can't be opened

![image](https://github.com/tareqimbasher/NetPad/assets/8684786/297046d3-d183-4120-b76f-c4423dd644b1)

If you get this message on macOS this means your system is configured to not allow unsigned apps to run. NetPad is not yet a signed application and on most macOS systems users are likely to receive this message.

To resolve this issue, open a terminal and execute:

```shell
xattr -cr /path/to/NetPad.app
```

NetPad will run normally afterwards. See [this discussion](https://github.com/tareqimbasher/NetPad/discussions/90) for a tip.

## NetPad does not start on Windows [Error: listen EACCES: permission denied]

![image](https://github.com/tareqimbasher/NetPad/assets/8684786/c8bb306c-355d-4004-b221-943b6d658f97)

If you get this message when starting NetPad on Windows, try these possible solutions:
1. Disable then re-enable one or more of your network interfaces. See issues [#159](https://github.com/tareqimbasher/NetPad/issues/159) and [#111](https://github.com/tareqimbasher/NetPad/issues/111).
2. Restart `winnat`. See [this SO answer](https://stackoverflow.com/a/67968597) for details.

## IntelliSense stops working

See the [Editor](https://github.com/tareqimbasher/NetPad/wiki/Editor) wiki page. If problems continue to happen, please open an issue.