# Downloads

### Official Installers

You can download officials installers for Windows, macOS and Linux from
the [Releases](https://github.com/tareqimbasher/NetPad/releases) page on GitHub.

NetPad has 2 release channels:

- **Stable**: The Electron.js version of NetPad. Installers that start with `netpad`
- **vNext**: Uses a native Rust-based shell. Installers that start with `netpad_vnext`

Both channels have the same feature set. The native vNext version is lighter on system resources and will eventually
become the main package. At which point, the Electron version will be deprecated.

### Unofficial Packages

These packages are maintained by community members.

| Installer                                                                                                                                                     | Channel    | Command                                              |
|---------------------------------------------------------------------------------------------------------------------------------------------------------------|------------|------------------------------------------------------|
| [![AUR Version](https://img.shields.io/aur/version/netpad-bin)](https://aur.archlinux.org/packages/netpad-bin)                                                | **stable** | `yay -S netpad-bin`                                  |
| [![WinGet Package Version](https://img.shields.io/winget/v/TareqImbasher.NetPad?color=%23097bbb)](https://winstall.app/apps/TareqImbasher.NetPad)             | **stable** | `winget install --id=TareqImbasher.NetPad  -e`       |
| [![WinGet Package Version](https://img.shields.io/winget/v/TareqImbasher.NetPad.vNext?color=%23097bbb)](https://winstall.app/apps/TareqImbasher.NetPad.vNext) | **vNext**  | `winget install --id=TareqImbasher.NetPad.vNext  -e` |

> Currently, the installers published in GitHub releases are the only installers published by NetPad maintainers.
> Unofficial packages are maintained by 3rd parties.

# Updates <!-- {docsify-ignore} -->

NetPad automatically checks for updates each time you start the application and will notify you when a new version is
available.

Automatic updates are not supported, but will be added in the future to make updating as seamless as possible. Stay
tuned for future updates!