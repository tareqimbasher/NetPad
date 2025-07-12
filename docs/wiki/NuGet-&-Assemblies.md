# NuGet

You can add a NuGet package using the Package Manager. 

:computer_mouse: To get to the Package Manager click the :wrench: icon (or press <kbd>F4</kbd>) to open your script's properties window then go to <kbd>Packages</kbd>.

The window is split into 3 columns:
* The left side column is your local Package Cache. These are NuGet packages you have already downloaded and exist on your computer.
* The middle column allows you to search and download new packages from the NuGet repository.
* The right column shows you information about a selected NuGet package.

#### Package Cache

NetPad downloads and caches NuGet packages into a custom directory. The location of this directory can be configured in <kbd><kbd>Settings</kbd> > <kbd>General</kbd> > <kbd>Folders</kbd> > <kbd>Package Cache</kbd></kbd>. You can purge this directory from the Package Manager.


> :bulb: Tip
> After adding a NuGet package, go to the <kbd>References</kbd> tab and select the package to easily add the namespaces you want to use in your script.

#### Adding NuGet Sources
NetPad will recognize additional NuGet sources added to your `Nuget.Config` file. There is no UI to manage these sources
in NetPad so far, so you'll have to add them manually. [More info](https://learn.microsoft.com/en-us/nuget/consume-packages/configuring-nuget-behavior#config-file-locations-and-uses)

# Assemblies

You can also reference and use your own assemblies.

:computer_mouse: Click the :wrench: icon (or press **F4**) to open your script's properties window and then go to <kbd>References</kbd>. Here you can click the <kbd>Browse</kbd> button to select an assembly from your computer and then directly use it in your script.

If your assembly depends on other assemblies, NetPad will attempt to load the dependencies from the same directory as the selected target assembly. You can also add the dependencies directly in <kbd>References</kbd>.

Feel free to mix and match referencing NuGet and local assemblies.

# `InternalsVisibleTo`

NetPad compiles your script into an assembly with the name "NetPadScript". To make the internals of your own assemblies visible to NetPad scripts add this attribute to your assembly:

```csharp
[InternalsVisibleTo("NetPadScript")]
```