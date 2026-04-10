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
    private static readonly Regex TemplateTokenRegex = new(@"\{(?<name>[A-Za-z0-9_]+)(:(?<format>[^}]+))?\}", RegexOptions.Compiled | RegexOptions.CultureInvariant);

    private readonly ObservableLoggerOptions _options;
    private readonly object _syncRoot = new();
    private readonly Regex? _retentionRegex;

    private StreamWriter? _writer;
    private string? _currentFilePath;
    private DateTimeOffset? _currentPeriodStart;
    private int _currentRollingCount = 1;

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
        UpdateRollingState(timestamp);
        string filePath = Path.Combine(_options.LogFolder, ResolveFileName(timestamp, _currentRollingCount));

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
        return ResolveFileName(timestamp, _currentRollingCount);
    }

    private string ResolveFileName(DateTimeOffset timestamp, int count)
    {
        if (!string.IsNullOrWhiteSpace(_options.FileNameTemplate))
        {
            return ApplyTemplate(_options.FileNameTemplate!, timestamp, count);
        }

        return string.IsNullOrWhiteSpace(_options.FileName)
            ? timestamp.ToString("yyyyMMdd'.log'", CultureInfo.InvariantCulture)
            : _options.FileName!;
    }

    private static string ApplyTemplate(string template, DateTimeOffset timestamp, int count)
    {
        return TemplateTokenRegex.Replace(template, match =>
        {
            string token = match.Groups["name"].Value;
            string format = match.Groups["format"].Success ? match.Groups["format"].Value : string.Empty;
            if (string.Equals(token, "Timestamp", StringComparison.Ordinal))
            {
                return string.IsNullOrWhiteSpace(format)
                    ? timestamp.ToString("O", CultureInfo.InvariantCulture)
                    : timestamp.ToString(format, CultureInfo.InvariantCulture);
            }

            if (string.Equals(token, "Count", StringComparison.Ordinal))
            {
                return string.IsNullOrWhiteSpace(format)
                    ? count.ToString(CultureInfo.InvariantCulture)
                    : count.ToString(format, CultureInfo.InvariantCulture);
            }

            return string.Empty;
        });
    }

    private void UpdateRollingState(DateTimeOffset timestamp)
    {
        DateTimeOffset currentPeriod = GetPeriodStart(timestamp);
        if (_currentPeriodStart == null)
        {
            _currentPeriodStart = currentPeriod;
            return;
        }

        if (_options.RollingInterval != RollingInterval.Infinite && currentPeriod != _currentPeriodStart.Value)
        {
            _currentPeriodStart = currentPeriod;
            _currentRollingCount++;
        }
    }

    private DateTimeOffset GetPeriodStart(DateTimeOffset timestamp)
    {
        return _options.RollingInterval switch
        {
            RollingInterval.Year => new DateTimeOffset(timestamp.Year, 1, 1, 0, 0, 0, timestamp.Offset),
            RollingInterval.Month => new DateTimeOffset(timestamp.Year, timestamp.Month, 1, 0, 0, 0, timestamp.Offset),
            RollingInterval.Day => new DateTimeOffset(timestamp.Year, timestamp.Month, timestamp.Day, 0, 0, 0, timestamp.Offset),
            RollingInterval.Hour => new DateTimeOffset(timestamp.Year, timestamp.Month, timestamp.Day, timestamp.Hour, 0, 0, timestamp.Offset),
            RollingInterval.Minute => new DateTimeOffset(timestamp.Year, timestamp.Month, timestamp.Day, timestamp.Hour, timestamp.Minute, 0, timestamp.Offset),
            _ => DateTimeOffset.MinValue,
        };
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
        bool hasCurrentFile = !string.IsNullOrWhiteSpace(_currentFilePath);
        if (hasCurrentFile && keepCount != int.MaxValue)
        {
            keepCount = Math.Max(keepCount - 1, 0);
        }
        TimeSpan? keepAge = _options.RetainedFileTimeLimit;

        int retentionIndex = 0;
        for (int i = 0; i < files.Length; i++)
        {
            FileInfo file = files[i];
            if (!string.IsNullOrWhiteSpace(_currentFilePath) &&
                string.Equals(file.FullName, _currentFilePath, StringComparison.OrdinalIgnoreCase))
            {
                continue;
            }

            bool exceedCount = retentionIndex >= keepCount;
            bool exceedAge = keepAge.HasValue && (utcNow - file.LastWriteTimeUtc) > keepAge.Value;
            if (!exceedCount && !exceedAge)
            {
                retentionIndex++;
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

        MatchCollection matches = TemplateTokenRegex.Matches(template);
        StringBuilder pattern = new();
        pattern.Append("^");

        int last = 0;
        foreach (Match match in matches)
        {
            if (match.Index > last)
            {
                pattern.Append(Regex.Escape(template.Substring(last, match.Index - last)));
            }

            string token = match.Groups["name"].Value;
            if (string.Equals(token, "Timestamp", StringComparison.Ordinal) || string.Equals(token, "Count", StringComparison.Ordinal))
            {
                pattern.Append("[0-9]+");
            }
            else
            {
                pattern.Append(".*?");
            }

            last = match.Index + match.Length;
        }

        if (last < template.Length)
        {
            pattern.Append(Regex.Escape(template.Substring(last)));
        }

        pattern.Append("$");
        return new Regex(pattern.ToString(), RegexOptions.Compiled | RegexOptions.CultureInvariant | RegexOptions.IgnoreCase);
    }
}
