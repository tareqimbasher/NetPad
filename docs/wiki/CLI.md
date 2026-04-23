# NetPad CLI: `npad`

`npad` brings the power of NetPad to your terminal. It lets you execute NetPad scripts or plain C# text files directly
from the command line, making it easy to automate tasks or integrate NetPad scripts into other tools and CI pipelines.

`npad` is a standalone tool, it does not require the NetPad desktop app to be installed.

**Usage:**

```
npad <command> [options]
```

**Commands:**

| Command                      | Description                                                |
|------------------------------|------------------------------------------------------------|
| [`run`](#npad-run)           | Run a script or a plain text file (default command)        |
| [`new`](#npad-new)           | Create a new script                                        |
| [`list`](#npad-list)         | List scripts in your library (alias: `ls`)                 |
| [`show`](#npad-show)         | Inspect a script's metadata and source code                |
| [`info`](#npad-info)         | Display information about NetPad and the environment       |
| [`cache`](#npad-cache)       | Manage the script build cache                              |
| [`logs`](#npad-logs)         | Manage NetPad log files (alias: `log`)                     |
| [`settings`](#npad-settings) | View or edit NetPad settings                               |

**Global options:** `-h` / `--help` (works with any command) for help, `--version` for version info.

> The `run` command is the default, you can omit it and just pass a script path or name directly (e.g. `npad myscript`).

## Quick Examples

```shell
npad myscript                          # run a script from your library by name
npad /path/to/script.netpad            # run a script by path
npad -e 'Console.WriteLine("Hi")'      # run inline code
echo 'var x = 1; x.Dump();' | npad     # pipe from stdin
npad new myscript                      # create a new script
npad list                              # list scripts in your library
npad cache clear                       # clear the build cache
```

# Installation

`npad` is published as a .NET tool. Requires the .NET SDK (6.0 or newer) to be installed.

**Install globally:**

```shell
dotnet tool install -g netpad.cli
```

**Update to the latest version:**

```shell
dotnet tool update -g netpad.cli
```

# Usage

## `npad run`

Runs a `.netpad` script or a plain text file. The `run` command is the default, so you can omit it:

```shell
npad myscript.cs                       # shorthand (implicit run)
npad run myscript.cs                   # explicit — same result
```

**Run a script by path**

```shell
npad /path/to/script.netpad
```

**Run a script that is saved in your script library by name**

```shell
npad report
```

This will look for scripts whose relative path contains "report" (case-insensitive). If exactly one match has a
filename (without extension) that equals "report", it will be selected automatically. Otherwise you will get a list to
choose from. For example, the command above would match scripts like:

- Report.netpad
- Report Jan.netpad
- Scripts/Reports/Summary.netpad

In this case, since `Report.netpad` has a filename that equals "report", it would be auto-selected. If `Report.netpad`
didn't exist, you'd be prompted to choose from the remaining matches.

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

`-e, --eval` specifies the code to run. In this case, the code will override the code inside "myscript.netpad".

```shell
npad run myscript.netpad --eval 'Console.WriteLine("Hello World")'
```

`-k, --kind` overrides the script kind. Supported values: `program`, `sql`.

```shell
npad run myscript.netpad --kind sql
```

`-s, --sdk` sets the target .NET SDK version.

```shell
npad run myscript.netpad --sdk 9
```

`-c, --connection` sets the [data connection](Data-Connections.md).

```shell
npad run myscript.netpad -c Chinook
```

`-O, --optimize` enables compiler optimizations.

```shell
npad run myscript.netpad -O
```

`--aspnet` references ASP.NET assemblies.

```shell
npad run myscript.netpad --aspnet
```

`-f, --format` changes how the output is formatted.

```shell
npad run myscript.netpad --format text      # Plain text format. Useful when piping to a file.
npad run myscript.netpad --format html      # Outputs HTML fragments
npad run myscript.netpad --format htmldoc   # Outputs a complete HTML document
npad run myscript.netpad --format json      # NDJSON (newline-delimited JSON)
```

`--sql` includes SQL queries in the output. Only applies to `--format json`.

```shell
npad run myscript.netpad --format json --sql
```

`-m, --minimal` reduces padding and metadata in output.

```shell
npad run myscript.netpad --minimal
```

`--no-cache` skips the build cache; does not use a cached build, if one exists, and does not cache the build from the
new run. More about caching [here](#caching).

```shell
npad run myscript.netpad --no-cache
```

`-b, --rebuild` rebuilds the script even if a cached build exists. Replaces the current cached build, if any.

```shell
npad run myscript.netpad --rebuild
```

`-v, --verbose` emits process logs. These are written to stderr so they are not included in the output when
redirecting (`>`).

```shell
npad run myscript.netpad --verbose
```

### Output

By default `npad` prints output to the console in a colored and structured format similar to what you would see when
dumping data in NetPad (GUI). You can select from other formats using the `--format` option:

- **text**: Uses a safe plain text format. Useful when piping to a file.
- **html**: Formats output as HTML fragments that can be injected into an HTML document.
- **htmldoc**: A complete HTML document with basic styling included.
- **json**: NDJSON (newline-delimited JSON). Each `Dump()` call produces one JSON line on stdout. Works natively with
  `jq` and other JSON tools. SQL output is excluded by default; use `--sql` to include it.

```shell
npad run myscript.netpad -f json | jq '.value'
npad run -c Chinook -e "Albums.Take(3)" -f json | jq '.value[].title'
```

### Exit Codes

`npad` returns meaningful exit codes so you can use it in CI pipelines and shell scripts:

| Exit Code | Meaning                                                              |
|-----------|----------------------------------------------------------------------|
| **0**     | Script ran successfully                                              |
| **1**     | Script started but failed (unhandled exception or non-zero exit)     |
| **2**     | Script never ran (compilation error, missing SDK, bad configuration) |
| **130**   | Script was cancelled                                                 |

```shell
npad run myscript.cs && echo "passed" || echo "failed"
```

### Plain Text Files

You can run any text file with C# code, even if it's not a valid `.netpad` script file.

```shell
npad run ./myfile.cs
npad run ./query.cs --connection Chinook
```

### Caching

`npad` builds your script before running it. The build is then cached and reused the next time you run the same script,
significantly speeding up subsequent runs.

The cached build will continue to be reused unless one of the following occurs:

1. **The script's code or configuration changes**.
2. **A cache-invalidating option is used** that differs from the last time the script ran.<br/>
   Examples include: `--sdk`, `--connection`, `--optimize`.<br/>
   Options such as `--format` or `--minimal`, on the other hand, do **not** require the script to be rebuilt and
   therefore do **not** invalidate the cache.
3. **A different version of `npad` CLI is used**.

You can always change caching options per-run using the `--no-cache` and `--rebuild` options explained above.

> See `npad cache -h` for cache management commands, and `npad info` to see the location of the build cache on disk.

**Custom cache location**

Set the `NETPAD_CACHE_DIR` environment variable to an absolute path to override where the build cache is stored. Run
`npad info` to confirm the path in use.

### Inline Code

You don't need a file to run C# code. The `--eval` (or `-e`) option allows you to run arbitrary code right from the
command-line. Since `run` is the default command, you can omit it:

```shell
npad -e 'Console.WriteLine("Hello World")'
```

You can mix in other options as well:

```shell
npad run -c Chinook -e "Albums.First()"
npad run -c Chinook -e "Albums.OrderBy(a => a.RecordSales)" -m
npad run -c Chinook -e "Employees.Where(c => c.MonthlySales > 10000)" -f json | jq '.value'
npad run -c Chinook -e "Employees.Where(c => c.MonthlySales > 10000)" -f htmldoc > index.html
npad run -e 'await WebApplication.CreateBuilder().Build().RunAsync("http://localhost:5000");' --aspnet -s 9 -O
npad run -e 'new DirectoryInfo("./").GetFiles("*.json").OrderBy(f => f.LastWriteTime).Select(f => new {f.Name, f.Length})' -O -m
```

You can also access the `Util` class in inline code:

```shell
npad run -e Util.Environment
```

> All inline scripts share a single cache directory. So if the code or a cache-invalidating option changes, a new build
> will be started.

### Piping from Stdin

You can pipe code to `npad` via stdin. When no script and no `--eval` option is provided and input is being piped,
`npad` reads from stdin:

```shell
echo 'Console.WriteLine("Hello World")' | npad
cat myscript.cs | npad -s 9
curl https://example.com/script.cs | npad -c Chinook
```

## `npad new`

Create a new script file. By default, creates a `.netpad` script in the current directory.

```shell
npad new myscript                     # creates ./myscript.netpad
npad new myscript.cs                  # creates a plain .cs file
npad new myscript -l                  # creates in your script library
npad new scripts/myscript -l          # creates in a subdirectory of your library
npad new ./path/to/myscript           # relative path from cwd
```

### Options

`-l, --library` creates the script in your script library instead of the current directory.

```shell
npad new myscript -l
```

`-k, --kind` sets the script kind. Supported values: `program` (default), `sql`.

```shell
npad new myquery --kind sql
```

`-s, --sdk` sets the target .NET SDK version.

```shell
npad new myscript -s 9
```

`-c, --connection` sets the [data connection](Data-Connections.md).

```shell
npad new myscript -c Chinook
```

`-O, --optimize` enables compiler optimizations.

```shell
npad new myscript -O
```

`--aspnet` references ASP.NET assemblies.

```shell
npad new myscript --aspnet
```

`-f, --force` overwrites an existing file without prompting.

```shell
npad new myscript -f
```

If a file with the same name already exists (and `--force` is not used), you will be prompted to overwrite, create with
a new name, or cancel.

## `npad list`

**alias:** `ls`

Lists all scripts found in your configured scripts library. You can configure the location of your scripts library via
[`npad settings`](#npad-settings) or through the NetPad GUI.

```shell
npad list
```

## `npad show`

Inspect a script's metadata and source code.

```shell
npad show /path/to/myscript.netpad

# or search in your library for a script with the name "myscript"
npad show myscript

# omit the argument to select from a list
npad show
```

## `npad info`

Prints information about NetPad and the current environment.

```shell
npad info
```

## `npad cache`

Manage the script build cache. See [Caching](#caching) for more details.

**List existing cached builds** (alias: `ls`)

```shell
npad cache list
npad cache ls          # alias
```

**Remove a cached build**

```shell
npad cache rm 1           # Remove by build number (from 'list' output)
npad cache rm myscript    # Remove by script name (case-insensitive)
npad cache clear          # Remove all cached builds
```

## `npad logs`

**alias:** `log`

Manage NetPad log files.

**List log files** (alias: `ls`)

```shell
npad logs              # default action
npad logs list
npad logs ls           # alias
```

**View a log file**

```shell
npad logs show 1       # Print log #1 to stdout
npad logs show 1 | grep ERROR
```

**Follow a log file (like tail -f)**

```shell
npad logs tail 1             # Follow log #1, showing the last 10 lines first
npad logs tail 1 -n 50       # Show the last 50 lines, then follow
```

**Remove log files**

```shell
npad logs rm 1         # Remove log #1 (from 'list' output)
npad logs clear        # Remove all log files
```

## `npad settings`

**View current settings (default)**

```shell
npad settings              # prints as table
npad settings show         # same
npad settings show --json  # prints as JSON (short: -j)
```

**Open settings in your default editor**

```shell
npad settings edit
```
