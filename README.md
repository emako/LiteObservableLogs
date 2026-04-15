[![NuGet](https://img.shields.io/nuget/v/LiteObservableLogs.svg)](https://nuget.org/packages/LiteObservableLogs) [![Actions](https://github.com/emako/LiteObservableLogs/actions/workflows/library.nuget.yml/badge.svg)](https://github.com/emako/LiteObservableLogs/actions/workflows/library.nuget.yml) 

# LiteObservableLogs

Lightweight observable logging with file output, Serilog-style fluent configuration, and `Microsoft.Extensions.Logging` integration—fewer moving parts, predictable behavior for desktop and library scenarios.

## Features

- **Fluent `LoggerConfiguration`** — chain `WriteTo`, `ObserveTo`, `Global`, `MinimumLevel`, and `Dispatcher` similar to common Serilog-style APIs.
- **`Microsoft.Extensions.Logging`** — register via `AddLiteObservableLogs` (with optional options callback or prebuilt `ObservableLoggerOptions`).
- **Static `Log` facade** — `Log.Information(...)`, `Log.Received`, and assignable `Log.Logger` for quick apps.
- **Multiple sinks** — file (rolling by time and/or size), console or `Debug`, in-process `Log.Received`, and `ObserveTo.Callback` with optional per-sink output templates.
- **Global output template** — `Global.OutputTemplate(...)` applies when a sink omits its own template (same idea as shared format strings).
- **Runtime tuning** — `Log.Logger.MinimumLevel` and `Log.Logger.OutputTemplate` are get/set and affect subsequent writes without rebuilding the logger.
- **Retention & collision handling** — optional retained file count/age; size rolling can back up a conflicting target path to `*.bak` when the file name template would collide (e.g. missing `{Count}`).

## Usage

### Fluent configuration

```csharp
using LiteObservableLogs;
using System.IO;

string logFile = Path.Combine(AppContext.BaseDirectory, "log", "observable_{Timestamp:yyyyMMdd}_{Count:d5}.log");

Log.Logger = new LoggerConfiguration()
    .WriteTo.File(
        logFile,
        rollingInterval: RollingInterval.Day,
        retainedFileCountLimit: 31,
        retainedFileTimeLimit: TimeSpan.FromDays(21),
        rollingSize: 10240L) // KB threshold for size-based roll
    .WriteTo.Console(target: ConsoleTarget.Debug)
    .ObserveTo.Event()
    .ObserveTo.Callback(e => { /* handle ObservableLogEvent */ }, outputTemplate: null)
    .Global.OutputTemplate("{Timestamp:yyyy-MM-dd HH:mm:ss.fff}|{Level:u5}|{Message}{NewLine}{Exception}")
    .Dispatcher.Async()
    .MinimumLevel.Debug()
    .CreateLogger();

Log.Information("Application started");
```

### Static `Log` and `Log.Received`

```csharp
using LiteObservableLogs;

Log.Received += (_, e) => Console.WriteLine(e.RenderedText);
Log.Warning("Something happened");
Log.CloseAndFlush();
```

### `Microsoft.Extensions.Logging` host

```csharp
using LiteObservableLogs.Providers;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

Host.CreateDefaultBuilder(args)
    .ConfigureLogging(builder =>
    {
        builder.ClearProviders();
        builder.AddLiteObservableLogs(options =>
        {
            options.LogFolder = Path.Combine(AppContext.BaseDirectory, "logs");
            options.FileName = "app.log";
            options.MinLevel = LogLevel.Information;
        });
    })
    .Build()
    .Run();
```

Optional compatibility helper (name only): `builder.AddSerilog()` maps to current `Log` options when you already use that pattern elsewhere.

### Output templates

- **Per sink** — pass `outputTemplate` on `WriteTo.File`, `WriteTo.Console`, `ObserveTo.Event`, or `ObserveTo.Callback`.
- **Global fallback** — `Global.OutputTemplate("...")` is used when a sink-specific template is omitted or null.
- **Tokens** — supports placeholders such as `{Timestamp}`, `{Level}`, `{Message}`, `{Exception}`, `{SourceContext}`, `{NewLine}`, `{StackFrames}`, caller and thread tokens, etc.

### `ObserveTo.Event` vs `ObserveTo.Callback`

- **`ObserveTo.Event`** — raises `Log.Received` with the formatted `ObservableLogEvent` (same isolation semantics as multicast handlers).
- **`ObserveTo.Callback`** — registers an `Action<ObservableLogEvent>`; remove with `Log.Logger.RemoveCallback(action)` when you no longer need it.

### Runtime `MinimumLevel` and `OutputTemplate`

```csharp
Log.Logger.MinimumLevel = LogLevel.Warning;
Log.Logger.OutputTemplate = "{Timestamp:O}|{Level:u3}|{Message}";
```

Changes apply to subsequent log writes (including async formatting where applicable).

### File name templates and rolling

- Use `{Timestamp:format}` and `{Count:format}` in `WriteTo.File` paths for time- and size-based rotation.
- If **size rolling** produces the same resolved file name as the active file (for example when `{Count}` is absent from the template), the library moves the existing file aside as **`original.log.bak`** (overwriting an existing `.bak`) before continuing.

## Requirements

- **Target frameworks** — `net48`, `netstandard2.0` (see `LiteObservableLogs.csproj`).
- **Dependencies** — `Microsoft.Extensions.Logging` / `Abstractions` (versions as referenced by the project).

## License

[MIT](LICENSE)

