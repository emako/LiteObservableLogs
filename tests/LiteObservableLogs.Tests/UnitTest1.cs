using System;
using System.IO;
using LiteObservableLogs.Providers;
using LiteLogLevel = LiteObservableLogs.LogLevel;
using Microsoft.Extensions.Logging;

namespace LiteObservableLogs.Tests;

public sealed class LiteObservableLogsTests
{
    [Fact]
    public void SyncFacadeWritesFilteredMessageToFile()
    {
        using TempDirectory temp = new();
        using (ObservableLoggerFacade logger = LoggerConfiguration.CreateDefault()
            .WriteToFile(temp.Path, "sync.log")
            .UseType(LoggerType.Sync)
            .UseLevel(LiteLogLevel.Information)
            .UseCategory("SyncCategory")
            .CreateLogger())
        {
            logger.Debug("ignored");
            logger.Information("hello", 42);
            logger.Flush();
        }

        string content = ReadAllTextShared(Path.Combine(temp.Path, "sync.log"));
        Assert.Contains("INFO", content);
        Assert.Contains("SyncCategory", content);
        Assert.Contains("hello 42", content);
        Assert.DoesNotContain("ignored", content);
    }

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

    [Fact]
    public void StaticLogCloseAndFlushPersistsAsyncEntries()
    {
        using TempDirectory temp = new();
        Log.Logger = LoggerConfiguration.CreateDefault()
            .WriteToFile(temp.Path, "static.log")
            .UseType(LoggerType.Async)
            .UseLevel(LiteLogLevel.Trace)
            .UseCategory("StaticCategory")
            .CreateLogger();

        Log.Warning("before close");
        Log.CloseAndFlush();

        string content = WaitForFileContent(Path.Combine(temp.Path, "static.log"));
        Assert.Contains("WARN", content);
        Assert.Contains("StaticCategory", content);
        Assert.Contains("before close", content);

        Log.Information("after close");
        string updatedContent = File.ReadAllText(Path.Combine(temp.Path, "static.log"));
        Assert.DoesNotContain("after close", updatedContent);
    }

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

    private static string WaitForFileContent(string path)
    {
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
        using FileStream stream = new(path, FileMode.Open, FileAccess.Read, FileShare.ReadWrite | FileShare.Delete);
        using StreamReader reader = new(stream);
        return reader.ReadToEnd();
    }

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
}
