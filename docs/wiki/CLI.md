# NetPad CLI: `npad`

`npad` brings the power of NetPad to your terminal. It lets you execute NetPad scripts or plain C# text files directly from the command line, making it easy to automate tasks or integrate NetPad scripts into other tools and CI pipelines.

```
npad [command] [options] [[--] <additional arguments>...]]

Options:
  -?, -h, --help  Show help and usage information
  --version       Show version information

Commands:
  run <PATH|NAME>  Run a script or a plain text file.
  list, ls         List scripts in your library.
  info             Display information about NetPad and the current environment.
  cat <PATH|NAME>  Inspect a script’s metadata and source code.
  cache            Show information about the script build cache.
  logs             Display NetPad log files.
  settings         View or edit NetPad settings.

Additional Arguments:
  Arguments passed to the script that is being run.
```

> Use `-h` with any command to see all available arguments, options and sub-commands.

# Installation

`npad` is published as a .NET tool. To install it globally:

```shell
dotnet tool install -g netpad.cli
```


# Commands

## `npad run`

Runs a `.netpad` script or a plain text file.

**Run a script by path**

```shell
npad run /path/to/script.netpad
```

**Run a script that is saved in your script library by name**

```shell
npad run report
```

This will look for a script with a path or name that matches "report" (case-insensitive). If multiple matches are found, you will get a list to select the script you want to run. For example, the command above will match scripts like:

- Report Jan.netpad
- Report Feb.netpad
- Scripts/Reports/Summary.netpad

**Select a script from a list**

Omit the path argument and `npad` will give you a list of all the scripts saved in your library to select one.

```shell
npad run
```

**Run a plain text file**

```shell
npad run HelloWorld.cs
```

### Options

`-x, --code` specifies the code to run. In this case, the code will override the code inside "myscript.netpad".

```shell
npad run myscript.netpad --code 'Console.WriteLine("Hello World")'
```

`--sdk` sets the target .NET SDK version.

```shell
npad run myscript.netpad --sdk 9
```

`--connection` sets the data connection.

```shell
npad run myscript.netpad --connection Chinook
```

`--optimize` enables compiler optimizations.

```shell
npad run myscript.netpad --optimize
```

`--aspnet` reference ASP.NET assemblies.

```shell
npad run myscript.netpad --aspnet
```

`--format` changes how the output is formatted.

```shell
npad run myscript.netpad --format text      # Plain text format; useful when piping to a file
npad run myscript.netpad --format html      # Outputs HTML fragments
npad run myscript.netpad --format htmldoc   # Outputs a complete HTML document
```

`--minimal` tries to reduce padding and metadata in the output for a more minimal format.

```shell
npad run myscript.netpad --minimal
```

`--no-cache` skips the build cache; does not use a cached build, if one exists, and does not cache the build from the new run. More about caching [here](#Caching).

```shell
npad run myscript.netpad --no-cache
```

`--rebuild` rebuilds the script even if a cached build exists. Replaces the current cached build, if any.

```shell
npad run myscript.netpad --rebuild
```

`--verbose` emits process logs. These are written to stderr (Console.Error) so they are not included in the output in case you are redirecting (>).

```shell
npad run myscript.netpad --verbose
```

### Plain Text Files

You can run any text file with C# code, even if its not a valid `.netpad` script file.

```shell
npad run ./myfile.cs
npad run ./query.cs --connection Chinook
```

### Caching

`npad` builds your script before running it. The build is then cached and reused the next time you run the same script, significantly speeding up subsequent runs.

The cached build will continue to be reused unless one of the following occurs:

1. **The script's code or configuration changes.**
2. **A cache-invalidating option is used** that differs from the last time the script ran.<br/>
   Examples include: `--sdk`, `--connection`, `--optimize`.<br/>
   Options such as `--format` or `--minimal`, on the other hand, do **not** require the script to be rebuilt and therefore do **not** invalidate the cache.
3. A different version of `npad` cli is used.

> See `npad cache -h` for cache management commands, and `npad info` to see the location of the build cache on disk.

### Inline Code

You don't need a file to run C# code. The `npad run -x` (or: `--code`) option allows you to run arbitrary code right from the command-line.

```shell
npad run -x 'Console.WriteLine("Hello World")'
```

You can mix in other options as well:

```shell
npad run --connection "Chinook" -x "Albums.First()"
npad run --connection "Chinook" -x "Albums.OrderBy(a => a.RecordSales)" --minimal
npad run --connection "Chinook" -x "Employees.Where(c => c.MonthlySales > 10000)" --format htmldoc > index.html
npad run -x 'await WebApplication.CreateBuilder().Build().RunAsync("http://localhost:5000");' --aspnet --sdk 9 --optimize
npad run -x 'new DirectoryInfo("./").GetFiles("*.json").OrderBy(f => f.LastWriteTime).Select(f => new {f.Name, f.Length})' --optimize --minimal
```

You can also access the `Util` class in inline code:

```shell
npad run -x Util.Environment
```

> All inline scripts share a single cache directory. So if the code, or a cache-invalidating option changes (ex: `--sdk`), a new build will be started.


## `npad list`

**alias:** `ls`

Lists all scripts found in your configured scripts library. You can configure the location of your scripts library from NetPad settings.

```shell
npad list
```


## `npad cat`

Inspect a script’s metadata and source code.

```shell
npad cat /path/to/myscript.netpad

# or search in your library for a script with the name "myscript"
npad cat myscript
```



## `npad info`

Prints information about NetPad and the current environment.

```shell
npad info
```



## `npad cache`

Shows information about the script build cache. See more about [caching](#caching) here.

**List existing cached builds**

```shell
npad cache ls
```

**Remove a cached build**

```shell
npad cache rm 1           # Remove a specific cached build 
npad run cache rm --all   # Remove all cached builds
```



## `npad logs`

Prints log files.

```shell
npad logs ls
```



## `npad settings`

**Open NetPad settings in your default editor**

```shell
npad settings
```

**Prints settings in a table format**

```shell
npad settings --table
```

**Prints settings in JSON format**

```shell
npad settings --json
```
