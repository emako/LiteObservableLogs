using System;
using System.IO;
using LiteObservableLogs.Providers;
using Microsoft.Extensions.Logging;

namespace LiteObservableLogs.Tests;

/// <summary>
/// End-to-end behavioral tests for facade, provider, and static entry usage.
/// </summary>
public sealed class LiteObservableLogsTests
{
    /// <summary>
    /// Verifies sync mode honors level filtering and writes expected payload.
    /// </summary>
    [Fact]
    public void SyncFacadeWritesFilteredMessageToFile()
    {
        using TempDirectory temp = new();
        using (ObservableLoggerFacade logger = LoggerConfiguration.CreateDefault()
            .WriteToFile(temp.Path, "sync.log")
            .UseType(LoggerType.Sync)
            .UseLevel(LogLevel.Information)
            .UseCategory("SyncCategory")
            .CreateLogger())
        {
            logger.Debug("ignored");
            logger.Information("hello", 42);
            // Force deterministic persistence before leaving the using scope.
            logger.Flush();
        }

        string content = ReadAllTextShared(Path.Combine(temp.Path, "sync.log"));
        Assert.Contains("INFO", content);
        Assert.Contains("SyncCategory", content);
        Assert.Contains("hello 42", content);
        Assert.DoesNotContain("ignored", content);
    }

    /// <summary>
    /// Verifies scope and category fields are included in provider-based logging.
    /// </summary>
    [Fact]
    public void ProviderWritesScopeAndCategory()
    {
        using TempDirectory temp = new();
        using (ObservableLoggerProvider provider = new(new ObservableLoggerOptions
        {
            LogFolder = temp.Path,
            FileName = "scope.log",
            LoggerType = LoggerType.Sync,
            IncludeScopes = true,
        }))
        using (ILoggerFactory factory = LoggerFactory.Create(builder => builder.AddProvider(provider)))
        {
            ILogger logger = factory.CreateLogger("ScopeCategory");
            using (logger.BeginScope("request-42"))
            {
                logger.LogInformation("inside scope");
            }

            provider.Flush();
        }

        string content = ReadAllTextShared(Path.Combine(temp.Path, "scope.log"));
        Assert.Contains("ScopeCategory", content);
        Assert.Contains("request-42", content);
        Assert.Contains("inside scope", content);
    }

    /// <summary>
    /// Verifies static logger shutdown flushes async queue and resets global logger.
    /// </summary>
    [Fact]
    public void StaticLogCloseAndFlushPersistsAsyncEntries()
    {
        using TempDirectory temp = new();
        Log.Logger = LoggerConfiguration.CreateDefault()
            .WriteToFile(temp.Path, "static.log")
            .UseType(LoggerType.Async)
            .UseLevel(LogLevel.Trace)
            .UseCategory("StaticCategory")
            .CreateLogger();

        Log.Warning("before close");
        Log.CloseAndFlush();

        string content = WaitForFileContent(Path.Combine(temp.Path, "static.log"));
        Assert.Contains("WARN", content);
        Assert.Contains("StaticCategory", content);
        Assert.Contains("before close", content);

        // After CloseAndFlush, static logger falls back to the no-op facade.
        Log.Information("after close");
        string updatedContent = File.ReadAllText(Path.Combine(temp.Path, "static.log"));
        Assert.DoesNotContain("after close", updatedContent);
    }

    /// <summary>
    /// Verifies exception helper renders both custom message and exception details.
    /// </summary>
    [Fact]
    public void FacadeExceptionIncludesExceptionDetails()
    {
        using TempDirectory temp = new();
        using (ObservableLoggerFacade logger = LoggerConfiguration.CreateDefault()
            .WriteToFile(temp.Path, "exception.log")
            .UseType(LoggerType.Sync)
            .UseCategory("ExceptionCategory")
            .CreateLogger())
        {
            InvalidOperationException exception = new("boom");
            logger.Exception(exception, "failure");
            logger.Flush();
        }

        string content = ReadAllTextShared(Path.Combine(temp.Path, "exception.log"));
        Assert.Contains("ERROR", content);
        Assert.Contains("failure", content);
        Assert.Contains("InvalidOperationException", content);
        Assert.Contains("boom", content);
    }

    /// <summary>
    /// Verifies file templates render timestamp, level, category, message, and exception text.
    /// </summary>
    [Fact]
    public void FileOutputTemplateIsRendered()
    {
        using TempDirectory temp = new();
        using (ObservableLoggerFacade logger = new LoggerConfiguration()
            .WriteTo.File(
                Path.Combine(temp.Path, "templated.log"),
                outputTemplate: "[{Timestamp:yyyy-MM-dd HH:mm:ss}] [{Level:u3}] {SourceContext}{NewLine}{Message}{NewLine}{Exception}")
            .UseType(LoggerType.Sync)
            .UseCategory("TemplateCategory")
            .MinimumLevel.Debug()
            .CreateLogger())
        {
            logger.Exception(new InvalidOperationException("boom"), "failure");
            logger.Flush();
        }

        string content = ReadAllTextShared(Path.Combine(temp.Path, "templated.log"));
        Assert.Contains("[ERR] TemplateCategory", content);
        Assert.Contains("failure", content);
        Assert.Contains("InvalidOperationException", content);
        Assert.Contains(Environment.NewLine, content);
    }

    /// <summary>
    /// Verifies console and event outputs each use their own configured templates.
    /// </summary>
    [Fact]
    public void ConsoleAndEventOutputTemplatesAreRendered()
    {
        TextWriter originalOut = Console.Out;
        using StringWriter console = new();
        Console.SetOut(console);

        ObservableLogEvent? received = null;
        EventHandler<ObservableLogEvent> handler = (_, entry) => received = entry;
        Log.Received += handler;

        try
        {
            using ObservableLoggerFacade logger = new LoggerConfiguration()
                .WriteTo.Console("CONSOLE|{Level:u3}|{SourceContext}|{Message}")
                .WriteTo.Event("EVENT|{Level:u3}|{SourceContext}|{Message}")
                .UseType(LoggerType.Sync)
                .UseCategory("ConsoleEventCategory")
                .MinimumLevel.Information()
                .CreateLogger();

            logger.Information("hello");
            logger.Flush();
        }
        finally
        {
            Log.Received -= handler;
            Console.SetOut(originalOut);
        }

        string consoleText = console.ToString();
        Assert.Contains("CONSOLE|INF|ConsoleEventCategory|hello", consoleText);
        Assert.NotNull(received);
        Assert.Equal("EVENT|INF|ConsoleEventCategory|hello", received!.RenderedText);
    }

    /// <summary>
    /// Verifies minute rolling creates new files and increments {Count} with numeric formatting.
    /// </summary>
    [Fact]
    public void MinuteRollingCreatesNewFileAndIncrementsCount()
    {
        using TempDirectory temp = new();
        StepClock clock = new(
            new DateTimeOffset(2026, 4, 10, 9, 0, 5, TimeSpan.Zero),
            new DateTimeOffset(2026, 4, 10, 9, 0, 30, TimeSpan.Zero),
            new DateTimeOffset(2026, 4, 10, 9, 1, 2, TimeSpan.Zero));

        using (ObservableLoggerFacade logger = new LoggerConfiguration()
            .WriteTo.File(
                Path.Combine(temp.Path, "rolling_{Timestamp:yyyyMMddHHmm}_{Count:D5}.log"),
                outputTemplate: "{Message}",
                rollingInterval: RollingInterval.Minute)
            .UseType(LoggerType.Sync)
            .UseOptions(options => options.TimestampProvider = clock.Next)
            .CreateLogger())
        {
            logger.Information("A");
            logger.Information("B");
            logger.Information("C");
            logger.Flush();
        }

        string file1 = Path.Combine(temp.Path, "rolling_202604100900_00001.log");
        string file2 = Path.Combine(temp.Path, "rolling_202604100901_00002.log");
        Assert.True(File.Exists(file1));
        Assert.True(File.Exists(file2));
        Assert.Contains("A", ReadAllTextShared(file1));
        Assert.Contains("B", ReadAllTextShared(file1));
        Assert.Contains("C", ReadAllTextShared(file2));
    }

    /// <summary>
    /// Verifies retained file count and age limits both clean up rolled files.
    /// </summary>
    [Fact]
    public void RetentionLimitsApplyToRolledFiles()
    {
        using TempDirectory temp = new();

        string stalePath = Path.Combine(temp.Path, "ret_200001010000_00000.log");
        File.WriteAllText(stalePath, "old");
        File.SetLastWriteTimeUtc(stalePath, DateTime.UtcNow - TimeSpan.FromDays(30));

        StepClock clock = new(
            new DateTimeOffset(2026, 4, 10, 9, 0, 1, TimeSpan.Zero),
            new DateTimeOffset(2026, 4, 10, 9, 1, 1, TimeSpan.Zero),
            new DateTimeOffset(2026, 4, 10, 9, 2, 1, TimeSpan.Zero),
            new DateTimeOffset(2026, 4, 10, 9, 3, 1, TimeSpan.Zero));

        using (ObservableLoggerFacade logger = new LoggerConfiguration()
            .WriteTo.File(
                Path.Combine(temp.Path, "ret_{Timestamp:yyyyMMddHHmm}_{Count:D5}.log"),
                outputTemplate: "{Message}",
                rollingInterval: RollingInterval.Minute,
                retainedFileCountLimit: 2,
                retainedFileTimeLimit: TimeSpan.FromDays(1))
            .UseType(LoggerType.Sync)
            .UseOptions(options => options.TimestampProvider = clock.Next)
            .CreateLogger())
        {
            logger.Information("1");
            logger.Information("2");
            logger.Information("3");
            logger.Information("4");
            logger.Flush();
        }

        Assert.False(File.Exists(stalePath));
        string[] files = Directory.GetFiles(temp.Path, "ret_*.log");
        Assert.Equal(2, files.Length);
    }

    /// <summary>
    /// Verifies Level:u3 follows Serilog short-level text.
    /// </summary>
    [Fact]
    public void LevelU3UsesSerilogStyleTokens()
    {
        using TempDirectory temp = new();
        using (ObservableLoggerFacade logger = new LoggerConfiguration()
            .WriteTo.File(
                Path.Combine(temp.Path, "u3.log"),
                outputTemplate: "{Level:u3}|{Message}")
            .UseType(LoggerType.Sync)
            .MinimumLevel.Trace()
            .CreateLogger())
        {
            logger.Trace("trace");
            logger.Debug("debug");
            logger.Information("info");
            logger.Warning("warn");
            logger.Error("error");
            logger.Critical("critical");
            logger.Flush();
        }

        string content = ReadAllTextShared(Path.Combine(temp.Path, "u3.log"));
        Assert.Contains("VRB|trace", content);
        Assert.Contains("DBG|debug", content);
        Assert.Contains("INF|info", content);
        Assert.Contains("WRN|warn", content);
        Assert.Contains("ERR|error", content);
        Assert.Contains("FTL|critical", content);
    }

    private static string WaitForFileContent(string path)
    {
        // Async logging can complete slightly later; poll briefly for stable content.
        for (int i = 0; i < 50; i++)
        {
            if (File.Exists(path))
            {
                string content = ReadAllTextShared(path);
                if (!string.IsNullOrWhiteSpace(content))
                {
                    return content;
                }
            }

            System.Threading.Thread.Sleep(20);
        }

        return File.Exists(path) ? ReadAllTextShared(path) : string.Empty;
    }

    private static string ReadAllTextShared(string path)
    {
        // Open with broad sharing to avoid races with active log writers.
        using FileStream stream = new(path, FileMode.Open, FileAccess.Read, FileShare.ReadWrite | FileShare.Delete);
        using StreamReader reader = new(stream);
        return reader.ReadToEnd();
    }

    /// <summary>
    /// Creates an isolated temp folder and deletes it on dispose.
    /// </summary>
    private sealed class TempDirectory : IDisposable
    {
        public TempDirectory()
        {
            Path = System.IO.Path.Combine(System.IO.Path.GetTempPath(), "LiteObservableLogs.Tests", Guid.NewGuid().ToString("N"));
            Directory.CreateDirectory(Path);
        }

        public string Path { get; }

        public void Dispose()
        {
            if (Directory.Exists(Path))
            {
                Directory.Delete(Path, true);
            }
        }
    }

    /// <summary>
    /// Deterministic clock used to drive rolling tests without waiting in real time.
    /// </summary>
    private sealed class StepClock
    {
        private readonly DateTimeOffset[] _values;
        private int _index;

        public StepClock(params DateTimeOffset[] values)
        {
            _values = values;
            _index = 0;
        }

        public DateTimeOffset Next()
        {
            if (_values.Length == 0)
            {
                return DateTimeOffset.Now;
            }

            if (_index >= _values.Length)
            {
                return _values[_values.Length - 1];
            }

            DateTimeOffset value = _values[_index];
            _index++;
            return value;
        }
    }
}
