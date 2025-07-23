## How to Get Help

If you need help with an issue you're facing:

- Take a look at the [Common Issues](#Common-Issues) section.
- [Search the issues](https://github.com/tareqimbasher/NetPad/issues) on GitHub, someone might've already run into the
  same problem and got it resolved.
- [Open an issue](https://github.com/tareqimbasher/NetPad/issues) on GitHub or join
  our [Discord server](https://discord.gg/FrgzNBYQFW).

## Common Issues

These are common issues that some users run into.

### NetPad cannot find my .NET SDK installation

See the [.NET SDK](/wiki/NET-SDK) page.

### IntelliSense or OmniSharp stops working

See the [Editor](/wiki/Editor?id=omnisharp-issues) wiki page.

### NetPad is damaged and can't be opened

![image](https://github.com/tareqimbasher/NetPad/assets/8684786/297046d3-d183-4120-b76f-c4423dd644b1)

If you get this message on macOS this means your system is configured to not allow unsigned apps to run. NetPad is not
yet a signed application and on most macOS systems users are likely to receive this message.

To resolve this issue, open a terminal and execute:

```shell
xattr -cr /path/to/NetPad.app
```

NetPad will run normally afterward. See [this discussion](https://github.com/tareqimbasher/NetPad/discussions/90) for a
tip.

### NetPad vNext is flagged as a Virus/Trojan on Windows

This is a [known upstream issue](https://github.com/tauri-apps/tauri/issues/2486) with Tauri, the framework the vNext
version is built on. To solve the issue add an exclusion to Windows Defender. There are **no** viruses or trojans built
into NetPad.

> :bulb: Also see [this page](/Download) about official vs non-official installers.

### NetPad does not start on Windows [Error: listen EACCES: permission denied]

![image](https://github.com/tareqimbasher/NetPad/assets/8684786/c8bb306c-355d-4004-b221-943b6d658f97)

If you get this message when starting NetPad on Windows, try these possible solutions:

1. Disable then re-enable one or more of your network interfaces. See
   issues [#159](https://github.com/tareqimbasher/NetPad/issues/159)
   and [#111](https://github.com/tareqimbasher/NetPad/issues/111).
2. Restart `winnat`. See [this SO answer](https://stackoverflow.com/a/67968597) for details.

