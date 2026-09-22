using System;
using System.IO;
using System.Text.RegularExpressions;
using Microsoft.Extensions.Logging;

namespace LiteObservableLogs.Tests;

/// <summary>
/// Verifies that rendered output lines can be rewritten by a caller-supplied transform.
/// </summary>
public sealed class OutputTransformTests
{
    /// <summary>
    /// Verifies file output is rewritten by <see cref="LoggerConfiguration.UseOutputTransform"/>.
    /// </summary>
    [Fact]
    public void UseOutputTransformRewritesFileOutput()
    {
        using TempDirectory temp = new();
        string filePath = Path.Combine(temp.Path, "mask.log");

        using (ObservableLoggerFacade logger = new LoggerConfiguration()
            .WriteTo.File(filePath, outputTemplate: "{Message}")
            .UseOutputTransform(static text => Regex.Replace(
                text,
                @"(?i)(password\s*=\s*)([^\s;]+)",
                "$1***"))
            .Dispatcher.Sync()
            .MinimumLevel.Information()
            .CreateLogger())
        {
            logger.Information("password=secret ok");
            logger.Flush();
        }

        string content = ReadAllTextShared(filePath);
        Assert.Contains("password=*** ok", content);
        Assert.DoesNotContain("password=secret", content);
    }

    /// <summary>
    /// Verifies omitting the transform leaves the rendered line unchanged.
    /// </summary>
    [Fact]
    public void WithoutTransformKeepsOriginalOutput()
    {
        using TempDirectory temp = new();
        string filePath = Path.Combine(temp.Path, "raw.log");

        using (ObservableLoggerFacade logger = new LoggerConfiguration()
            .WriteTo.File(filePath, outputTemplate: "{Message}")
            .Dispatcher.Sync()
            .MinimumLevel.Information()
            .CreateLogger())
        {
            logger.Information("password=secret");
            logger.Flush();
        }

        Assert.Contains("password=secret", ReadAllTextShared(filePath));
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
