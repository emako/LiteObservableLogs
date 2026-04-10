using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using System.Collections.ObjectModel;
using System.Windows;

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
    private void LogTrace()
    {
        Log.Trace("Trace: Button test");
    }

    [RelayCommand]
    private void LogDebug()
    {
        Log.Debug($"Debug: Button test");
    }

    [RelayCommand]
    private void LogInformation()
    {
        Log.Information("Information: Button test");
    }

    [RelayCommand]
    private void LogWarning()
    {
        Log.Warning("Warning: Button test");
    }

    [RelayCommand]
    private void LogError()
    {
        Log.Error("Error: Button test");
    }

    [RelayCommand]
    private void LogCritical()
    {
        Log.Critical("Critical: Button test");
    }

    [RelayCommand]
    private void LogException()
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
}
