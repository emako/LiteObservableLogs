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
    private static readonly UTF8Encoding Utf8NoBom = new(encoderShouldEmitUTF8Identifier: false);
    private static readonly int NewLineByteCount = Utf8NoBom.GetByteCount(Environment.NewLine);

    private readonly ObservableLoggerOptions _options;
    private readonly object _syncRoot = new();
    private readonly Regex? _retentionRegex;
    private readonly Regex? _countExtractionRegex;

    private StreamWriter? _writer;
    private string? _currentFilePath;
    private DateTimeOffset? _currentPeriodStart;
    private int _currentRollingCount = 1;
    private bool _rollingCountInitialized;
    private long _currentFileLengthBytes;

    /// <summary>
    /// Prepares regex helpers derived from <see cref="ObservableLoggerOptions.FileNameTemplate"/> for retention and rolling count.
    /// </summary>
    public ObservableFileWriter(ObservableLoggerOptions options)
    {
        _options = options;
        _retentionRegex = BuildRetentionRegex(_options.FileNameTemplate);
        _countExtractionRegex = BuildCountExtractionRegex(_options.FileNameTemplate);
    }

    /// <summary>
    /// Gets the active file path currently opened by the writer, or <c>null</c> before the first write.
    /// </summary>
    public string? CurrentLogFilePath
    {
        get
        {
            lock (_syncRoot)
            {
                return _currentFilePath;
            }
        }
    }

    /// <summary>
    /// Appends one formatted message line to the active output file.
    /// </summary>
    public void WriteLine(DateTimeOffset timestamp, string message)
    {
        lock (_syncRoot)
        {
            EnsureWriter(timestamp, message);
            _writer!.WriteLine(message);
            _currentFileLengthBytes += GetEstimatedWriteBytes(message);
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

    /// <summary>Creates or rotates the underlying <see cref="StreamWriter"/> when the target path changes.</summary>
    private void EnsureWriter(DateTimeOffset timestamp, string pendingMessage)
    {
        Directory.CreateDirectory(_options.LogFolder);
        InitializeRollingCountFromExistingFiles();
        UpdateRollingStateByInterval(timestamp);
        if (ShouldRollBySize(pendingMessage))
        {
            _currentRollingCount++;
        }

        string filePath = Path.Combine(_options.LogFolder, ResolveFileName(timestamp, _currentRollingCount));

        if (string.Equals(filePath, _currentFilePath, StringComparison.OrdinalIgnoreCase) && _writer != null)
        {
            return;
        }

        // Reopen when target path changes (for date rolling) or writer is missing.
        _writer?.Flush();
        _writer?.Dispose();

        FileStream stream = new(filePath, FileMode.Append, FileAccess.Write, FileShare.ReadWrite);
        _writer = new StreamWriter(stream, Utf8NoBom)
        {
            AutoFlush = false,
        };

        _currentFilePath = filePath;
        _currentFileLengthBytes = stream.Length;
        CleanupOldFiles();
    }

    /// <summary>
    /// Scans the log folder once so <c>{Count}</c> templates continue numbering after restart.
    /// </summary>
    private void InitializeRollingCountFromExistingFiles()
    {
        if (_rollingCountInitialized)
        {
            return;
        }

        _rollingCountInitialized = true;
        if (_countExtractionRegex == null || !Directory.Exists(_options.LogFolder))
        {
            return;
        }

        int maxCount = 0;
        FileInfo[] files = new DirectoryInfo(_options.LogFolder).GetFiles("*", SearchOption.TopDirectoryOnly);
        for (int i = 0; i < files.Length; i++)
        {
            Match match = _countExtractionRegex.Match(files[i].Name);
            if (!match.Success)
            {
                continue;
            }

            Group countGroup = match.Groups["count"];
            if (!countGroup.Success || !int.TryParse(countGroup.Value, NumberStyles.Integer, CultureInfo.InvariantCulture, out int parsed))
            {
                continue;
            }

            if (parsed > maxCount)
            {
                maxCount = parsed;
            }
        }

        if (maxCount > 0)
        {
            _currentRollingCount = maxCount;
        }
    }

    /// <summary>Applies template or fixed <see cref="ObservableLoggerOptions.FileName"/> rules.</summary>
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

    /// <summary>Expands <c>{Timestamp}</c> and <c>{Count}</c> placeholders in a file name template.</summary>
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

    /// <summary>Bumps the rolling counter when the calendar period advances.</summary>
    private void UpdateRollingStateByInterval(DateTimeOffset timestamp)
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

    /// <summary>Rolls to next count when current file plus next write would exceed configured size limit.</summary>
    private bool ShouldRollBySize(string pendingMessage)
    {
        if (_options.RollingSize <= 0 || _writer == null)
        {
            return false;
        }

        long sizeLimitBytes = _options.RollingSize * 1024L;
        if (sizeLimitBytes <= 0)
        {
            return false;
        }

        long estimatedBytes = GetEstimatedWriteBytes(pendingMessage);
        if (estimatedBytes <= 0)
        {
            return false;
        }

        // Avoid rolling an empty/new file repeatedly when a single large message exceeds the threshold.
        if (_currentFileLengthBytes == 0)
        {
            return false;
        }

        return _currentFileLengthBytes + estimatedBytes > sizeLimitBytes;
    }

    private static int GetEstimatedWriteBytes(string message)
    {
        return Utf8NoBom.GetByteCount(message) + NewLineByteCount;
    }

    /// <summary>Maps an instant to the start of the configured <see cref="RollingInterval"/> period.</summary>
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

    /// <summary>Deletes older files according to count and age limits, never deleting the active file.</summary>
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

        FileInfo[] files = [.. new DirectoryInfo(_options.LogFolder)
            .GetFiles("*", SearchOption.TopDirectoryOnly)
            .Where(static file => !file.Attributes.HasFlag(FileAttributes.Directory))
            .Where(IsRetentionCandidate)
            .OrderByDescending(file => file.LastWriteTimeUtc)];

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
            catch (Exception ex)
            {
                // Ignore retention cleanup failures to avoid affecting primary log writes.
#if DEBUG
                System.Diagnostics.Debug.WriteLine($"[LiteObservableLogs] Retention cleanup skipped '{file.FullName}': {ex}");
#endif
            }
        }
    }

    /// <summary>Whether a directory entry should participate in retention (matches template or fixed name rules).</summary>
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

    /// <summary>Builds a regex that matches log files produced from the given template for safe cleanup.</summary>
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
                pattern.Append(Regex.Escape(template!.Substring(last, match.Index - last)));
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

        if (last < template!.Length)
        {
            pattern.Append(Regex.Escape(template.Substring(last)));
        }

        pattern.Append("$");
        return new Regex(pattern.ToString(), RegexOptions.Compiled | RegexOptions.CultureInvariant | RegexOptions.IgnoreCase);
    }

    /// <summary>Builds a regex that captures the numeric <c>{Count}</c> segment from existing file names.</summary>
    private static Regex? BuildCountExtractionRegex(string? template)
    {
        if (string.IsNullOrWhiteSpace(template))
        {
            return null;
        }

        MatchCollection matches = TemplateTokenRegex.Matches(template);
        StringBuilder pattern = new();
        pattern.Append("^");

        bool hasCount = false;
        int last = 0;
        foreach (Match match in matches)
        {
            if (match.Index > last)
            {
                pattern.Append(Regex.Escape(template!.Substring(last, match.Index - last)));
            }

            string token = match.Groups["name"].Value;
            if (string.Equals(token, "Count", StringComparison.Ordinal))
            {
                pattern.Append("(?<count>[0-9]+)");
                hasCount = true;
            }
            else if (string.Equals(token, "Timestamp", StringComparison.Ordinal))
            {
                pattern.Append("[0-9]+");
            }
            else
            {
                pattern.Append(".*?");
            }

            last = match.Index + match.Length;
        }

        if (last < template!.Length)
        {
            pattern.Append(Regex.Escape(template.Substring(last)));
        }

        pattern.Append("$");
        if (!hasCount)
        {
            return null;
        }

        return new Regex(pattern.ToString(), RegexOptions.Compiled | RegexOptions.CultureInvariant | RegexOptions.IgnoreCase);
    }
}
