using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Microsoft.Extensions.Logging;
using System.Collections.ObjectModel;

namespace LiteObservableLogs.Demo.WPF.ViewModels;

public sealed partial class MainViewModel : ObservableObject, IDisposable
{
    private readonly ILogger<MainViewModel> _logger;

    public ObservableCollection<string> LiveLines { get; } = new();

    public MainViewModel(ILogger<MainViewModel> logger)
    {
        _logger = logger;
        Log.Received += OnLogReceived;
    }

    public void Dispose()
    {
        Log.Received -= OnLogReceived;
    }

    private void OnLogReceived(object? sender, ObservableLogEvent e)
    {
        string line = e.RenderedText;
        System.Windows.Application.Current?.Dispatcher.Invoke(() =>
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
        _logger.LogTrace("Trace: Button test {Time}", DateTime.Now.ToString("HH:mm:ss.fff"));
    }

    [RelayCommand]
    private void LogDebug()
    {
        _logger.LogDebug("Debug: Button test {Time}", DateTime.Now.ToString("HH:mm:ss.fff"));
    }

    [RelayCommand]
    private void LogInformation()
    {
        _logger.LogInformation("Information: Button test {Time}", DateTime.Now.ToString("HH:mm:ss.fff"));
    }

    [RelayCommand]
    private void LogWarning()
    {
        _logger.LogWarning("Warning: Button test {Time}", DateTime.Now.ToString("HH:mm:ss.fff"));
    }

    [RelayCommand]
    private void LogError()
    {
        _logger.LogError("Error: Button test {Time}", DateTime.Now.ToString("HH:mm:ss.fff"));
    }

    [RelayCommand]
    private void LogCritical()
    {
        _logger.LogCritical("Critical: Button test {Time}", DateTime.Now.ToString("HH:mm:ss.fff"));
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
            _logger.LogError(ex, "Exception: Written to log after catch");
        }
    }
}
