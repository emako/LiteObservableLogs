using System;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;

namespace LiteObservableLogs.Internal;

/// <summary>
/// Handles thread-safe file stream lifecycle and date-based file switching.
/// </summary>
internal sealed class ObservableFileWriter : IDisposable
{
    private readonly ObservableLoggerOptions _options;
    private readonly object _syncRoot = new();
    private readonly Regex? _retentionRegex;

    private StreamWriter? _writer;
    private string? _currentFilePath;

    public ObservableFileWriter(ObservableLoggerOptions options)
    {
        _options = options;
        _retentionRegex = BuildRetentionRegex(_options.FileNameTemplate);
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
        CleanupOldFiles();
    }

    private string ResolveFileName(DateTimeOffset timestamp)
    {
        if (!string.IsNullOrWhiteSpace(_options.FileNameTemplate))
        {
            return ApplyTemplate(_options.FileNameTemplate!, timestamp);
        }

        return string.IsNullOrWhiteSpace(_options.FileName)
            ? timestamp.ToString("yyyyMMdd'.log'", CultureInfo.InvariantCulture)
            : _options.FileName!;
    }

    private static string ApplyTemplate(string template, DateTimeOffset timestamp)
    {
        return Regex.Replace(
            template,
            @"\{Timestamp:([^}]+)\}",
            match => timestamp.ToString(match.Groups[1].Value, CultureInfo.InvariantCulture),
            RegexOptions.CultureInvariant);
    }

    private void CleanupOldFiles()
    {
        if (_options.RetainedFileCountLimit == null && _options.RetainedFileTimeLimit == null)
        {
            return;
        }

        if (!Directory.Exists(_options.LogFolder))
        {
            return;
        }

        FileInfo[] files = new DirectoryInfo(_options.LogFolder)
            .GetFiles("*", SearchOption.TopDirectoryOnly)
            .Where(static file => !file.Attributes.HasFlag(FileAttributes.Directory))
            .Where(IsRetentionCandidate)
            .OrderByDescending(file => file.LastWriteTimeUtc)
            .ToArray();

        DateTime utcNow = DateTime.UtcNow;
        int keepCount = _options.RetainedFileCountLimit ?? int.MaxValue;
        TimeSpan? keepAge = _options.RetainedFileTimeLimit;

        for (int i = 0; i < files.Length; i++)
        {
            FileInfo file = files[i];
            bool exceedCount = i >= keepCount;
            bool exceedAge = keepAge.HasValue && (utcNow - file.LastWriteTimeUtc) > keepAge.Value;
            if (!exceedCount && !exceedAge)
            {
                continue;
            }

            try
            {
                file.Delete();
            }
            catch
            {
                // Ignore retention cleanup failures to avoid affecting primary log writes.
            }
        }
    }

    private bool IsRetentionCandidate(FileInfo file)
    {
        if (string.IsNullOrWhiteSpace(_options.FileNameTemplate))
        {
            return string.IsNullOrWhiteSpace(_options.FileName)
                ? file.Extension.Equals(".log", StringComparison.OrdinalIgnoreCase)
                : string.Equals(file.Name, _options.FileName, StringComparison.OrdinalIgnoreCase);
        }

        return _retentionRegex?.IsMatch(file.Name) == true;
    }

    private static Regex? BuildRetentionRegex(string? template)
    {
        if (string.IsNullOrWhiteSpace(template))
        {
            return null;
        }

        string pattern = Regex.Escape(template)
            .Replace(@"\{Timestamp\:", "{Timestamp:")
            .Replace(@"\}", "}");
        pattern = Regex.Replace(
            pattern,
            @"\{Timestamp:[^}]+\}",
            "[0-9]+",
            RegexOptions.CultureInvariant);

        return new Regex($"^{pattern}$", RegexOptions.Compiled | RegexOptions.CultureInvariant | RegexOptions.IgnoreCase);
    }
}
