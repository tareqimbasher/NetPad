# Referencing Assemblies

NetPad supports adding NuGet packages and assembly references to your scripts.

## NuGet

Add a NuGet package to your script using the Package Manager.

:computer_mouse: To get to the Package Manager click the :wrench: icon (or press <kbd>F4</kbd>) to open your script's
properties window then go to the <kbd>Packages</kbd> tab.

The tab is split into 3 columns:

* The **left column** is your local Package Cache. These are NuGet packages you have already downloaded and exist in the
  cache directory on your computer.
* The **middle column** allows you to search and download new packages from NuGet repositories.
* The **right column** shows you information about the selected NuGet package.

Once you've added the package to your script, go to the <kbd>References</kbd> tab and select the package. This will give
you a list of all the available namespaces contained in the assembly. Selecting a namespace will automatically add it to
your script's namespaces.

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
References</kbd>. Click the <kbd>Browse</kbd> button to select an assembly from your computer to add it to your script.

If your assembly depends on other assemblies, NetPad will attempt to load them as long as the dependencies are in the
same directory as the selected target assembly. You can also add the dependencies directly in <kbd>References</kbd>.

## `InternalsVisibleTo`

NetPad compiles your script into an assembly with the name "NetPadScript". To make the internals of your own assemblies
visible to NetPad scripts add this attribute to your assembly:

```csharp
[InternalsVisibleTo("NetPadScript")]
```