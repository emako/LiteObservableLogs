using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Microsoft.Extensions.Logging;
using System.Collections.ObjectModel;
using System.IO;
using System.Windows;
using Windows.Storage;
using Windows.System;

namespace LiteObservableLogs.Demo.WPF;

public sealed partial class MainViewModel : ObservableObject, IDisposable
{
    public ObservableCollection<string> LiveLines { get; } = [];

    public MainViewModel()
    {
        Log.Received += OnLogReceived;
    }

    public void Dispose()
    {
        Log.Received -= OnLogReceived;
    }

    private void OnLogReceived(object? sender, ObservableLogEvent e)
    {
        string line = e.RenderedText;
        Application.Current?.Dispatcher.Invoke(() =>
        {
            LiveLines.Insert(0, line);
            while (LiveLines.Count > 200)
            {
                LiveLines.RemoveAt(LiveLines.Count - 1);
            }
        });
    }

    [RelayCommand]
    private static void LogTrace()
    {
        Log.Trace("Trace: Button test");
    }

    [RelayCommand]
    private static void LogDebug()
    {
        // Test local method only in debug ...

        Write();

        static void Write()
        {
            Log.Debug($"Debug: Button test");
        }
    }

    [RelayCommand]
    private static void LogInformation()
    {
        Log.Information("Information: Button test");
    }

    [RelayCommand]
    private static void LogWarning()
    {
        // Test scope only in warning ...
        // Record additional properties in the scope; these will be included in the log entry and can be used for filtering and enrichment.
        using var _ = Log.Logger.InnerLogger.BeginScope("Action={Action};OperationId={OperationId}", "LogWarning", Guid.NewGuid().ToString("N")[..8]);

        Log.Warning("Warning: Button test");
    }

    [RelayCommand]
    private static void LogError()
    {
        Log.Error("Error: Button test");
    }

    [RelayCommand]
    private static void LogCritical()
    {
        Log.Critical("Critical: Button test");
    }

    [RelayCommand]
    private static void LogException()
    {
        try
        {
            throw new InvalidOperationException("Demo exception message");
        }
        catch (Exception ex)
        {
            Log.Exception(ex, "Exception: Written to log after catch");
        }
    }

    [RelayCommand]
    private static async Task OpenCurrentLogDirectoryAsync()
    {
        string? folderPath = GetCurrentLogFolderPath();
        if (string.IsNullOrWhiteSpace(folderPath))
        {
            MessageBox.Show("Current log directory is not available.", "LiteObservableLogs Demo", MessageBoxButton.OK, MessageBoxImage.Warning);
            return;
        }

        try
        {
            StorageFolder folder = await StorageFolder.GetFolderFromPathAsync(folderPath);
            bool launched = await Launcher.LaunchFolderAsync(folder);
            if (!launched)
            {
                MessageBox.Show("The system could not open the current log directory.", "LiteObservableLogs Demo", MessageBoxButton.OK, MessageBoxImage.Warning);
            }
        }
        catch (Exception ex)
        {
            Log.Warning("OpenCurrentLogDirectory failed: {Message}", ex.Message);
            MessageBox.Show($"Open log directory failed.\n{ex.Message}", "LiteObservableLogs Demo", MessageBoxButton.OK, MessageBoxImage.Warning);
        }
    }

    [RelayCommand]
    private static async Task OpenCurrentLogDirectoryAndSelectAsync()
    {
        string? folderPath = GetCurrentLogFolderPath();
        if (string.IsNullOrWhiteSpace(folderPath))
        {
            MessageBox.Show("Current log directory is not available.", "LiteObservableLogs Demo", MessageBoxButton.OK, MessageBoxImage.Warning);
            return;
        }

        string? currentLogFilePath = GetCurrentLogFilePath();
        if (string.IsNullOrWhiteSpace(currentLogFilePath))
        {
            MessageBox.Show("No log file is available to select yet. Write at least one log entry first.", "LiteObservableLogs Demo", MessageBoxButton.OK, MessageBoxImage.Information);
            return;
        }

        try
        {
            StorageFolder folder = await StorageFolder.GetFolderFromPathAsync(folderPath);
            StorageFile file = await StorageFile.GetFileFromPathAsync(currentLogFilePath);
            FolderLauncherOptions options = new();
            options.ItemsToSelect.Add(file);

            bool launched = await Launcher.LaunchFolderAsync(folder, options);
            if (!launched)
            {
                MessageBox.Show("The system could not open the directory and select the current log file.", "LiteObservableLogs Demo", MessageBoxButton.OK, MessageBoxImage.Warning);
            }
        }
        catch (Exception ex)
        {
            Log.Warning("OpenCurrentLogDirectoryAndSelect failed: {Message}", ex.Message);
            MessageBox.Show($"Open log directory and select file failed.\n{ex.Message}", "LiteObservableLogs Demo", MessageBoxButton.OK, MessageBoxImage.Warning);
        }
    }

    private static string? GetCurrentLogFolderPath()
    {
        string? logFolder = Log.Logger.LogFolder;
        if (!string.IsNullOrWhiteSpace(logFolder) && Directory.Exists(logFolder))
        {
            return logFolder;
        }

        string? currentLogFilePath = Log.Logger.CurrentLogFilePath;
        if (!string.IsNullOrWhiteSpace(currentLogFilePath))
        {
            string? folder = Path.GetDirectoryName(currentLogFilePath);
            if (!string.IsNullOrWhiteSpace(folder) && Directory.Exists(folder))
            {
                return folder;
            }
        }

        return null;
    }

    private static string? GetCurrentLogFilePath()
    {
        string? current = Log.Logger.CurrentLogFilePath;
        if (!string.IsNullOrWhiteSpace(current) && File.Exists(current))
        {
            return current;
        }

        string? folderPath = GetCurrentLogFolderPath();
        if (string.IsNullOrWhiteSpace(folderPath))
        {
            return null;
        }

        return Directory.EnumerateFiles(folderPath, "*.log", SearchOption.TopDirectoryOnly)
            .OrderByDescending(File.GetLastWriteTimeUtc)
            .FirstOrDefault();
    }
}
