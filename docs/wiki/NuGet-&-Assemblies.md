# Referencing Assemblies

NetPad supports adding NuGet packages and assembly references to your scripts.

## NuGet

Add a NuGet package to your script using the Package Manager.

:computer_mouse: To get to the Package Manager click the :wrench: icon (or press <kbd>F4</kbd>) to open your script's
properties window then go to the <kbd>Packages</kbd> page. The <kbd>refs</kbd> chip in the script's toolbar opens the
same window on the <kbd>References</kbd> page.

The page has a package list on the left and the selected package's details on the right. Above the list, choose where
the packages come from:

* **Feeds** searches your NuGet sources. Turn on **Pre-release** to include pre-release versions.
* **local cache** lists the packages you have already downloaded to the cache directory on your computer. Turn on
  **Dependencies** to also show packages that were downloaded as dependencies of other packages.

Select a package to see its details, versions and dependencies, then click **Reference _version_** to add it to your
script, or **Download only** to put it in the cache without referencing it.

Once you've added the package to your script, go to the <kbd>References</kbd> page and select the package. This will
give you a list of all the available namespaces contained in the assembly. Checked namespaces are added to your script's
namespaces.

#### Package Cache Directory

NetPad downloads and caches NuGet packages into a custom directory on your computer. The location of this directory can
be configured in <kbd><kbd>Settings</kbd> > <kbd>General</kbd> > <kbd>Folders</kbd> > <kbd>Package Cache</kbd></kbd>.
You can purge (clear) this directory from the Package Manager.

#### Adding NuGet Sources

NetPad will recognize additional NuGet sources added to your `Nuget.Config` file. There is no UI to manage these sources
in NetPad, but you can add them manually.
See [this](https://learn.microsoft.com/en-us/nuget/consume-packages/configuring-nuget-behavior#config-file-locations-and-uses)
for the default location of your `Nuget.Config` file.

## Assemblies

You can also reference and use your own assemblies.

:computer_mouse: Click the :wrench: icon (or press **F4**) to open your script's properties window and then go to <kbd>
References</kbd>. Click **Browse assemblies…** to select an assembly from your computer to add it to your script. This
needs the desktop app: the browser version of NetPad cannot pick files from your computer.

If your assembly depends on other assemblies, NetPad will attempt to load them as long as the dependencies are in the
same directory as the selected target assembly. You can also add the dependencies directly in <kbd>References</kbd>.

## `InternalsVisibleTo`

NetPad compiles your script into an assembly with the name "NetPadScript". To make the internals of your own assemblies
visible to NetPad scripts add this attribute to your assembly:

```csharp
[InternalsVisibleTo("NetPadScript")]
```