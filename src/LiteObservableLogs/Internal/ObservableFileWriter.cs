using System;
using System.Globalization;
using System.IO;
using System.Text;

namespace LiteObservableLogs.Internal;

/// <summary>
/// Handles thread-safe file stream lifecycle and date-based file switching.
/// </summary>
internal sealed class ObservableFileWriter : IDisposable
{
    private readonly ObservableLoggerOptions _options;
    private readonly object _syncRoot = new();

    private StreamWriter? _writer;
    private string? _currentFilePath;

    public ObservableFileWriter(ObservableLoggerOptions options)
    {
        _options = options;
    }

    /// <summary>
    /// Appends one formatted message line to the active output file.
    /// </summary>
    public void WriteLine(DateTimeOffset timestamp, string message)
    {
        lock (_syncRoot)
        {
            EnsureWriter(timestamp);
            _writer!.WriteLine(message);
        }
    }

    /// <summary>
    /// Flushes the active writer, if any.
    /// </summary>
    public void Flush()
    {
        lock (_syncRoot)
        {
            _writer?.Flush();
        }
    }

    /// <summary>
    /// Flushes and releases current file resources.
    /// </summary>
    public void Dispose()
    {
        lock (_syncRoot)
        {
            _writer?.Flush();
            _writer?.Dispose();
            _writer = null;
            _currentFilePath = null;
        }
    }

    private void EnsureWriter(DateTimeOffset timestamp)
    {
        Directory.CreateDirectory(_options.LogFolder);
        string filePath = Path.Combine(_options.LogFolder, ResolveFileName(timestamp));

        if (string.Equals(filePath, _currentFilePath, StringComparison.OrdinalIgnoreCase) && _writer != null)
        {
            return;
        }

        // Reopen when target path changes (for date rolling) or writer is missing.
        _writer?.Flush();
        _writer?.Dispose();

        FileStream stream = new(filePath, FileMode.Append, FileAccess.Write, FileShare.ReadWrite);
        _writer = new StreamWriter(stream, new UTF8Encoding(false))
        {
            AutoFlush = false,
        };

        _currentFilePath = filePath;
    }

    private string ResolveFileName(DateTimeOffset timestamp)
    {
        return string.IsNullOrWhiteSpace(_options.FileName)
            ? timestamp.ToString("yyyyMMdd'.log'", CultureInfo.InvariantCulture)
            : _options.FileName!;
    }
}
