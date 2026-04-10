using Microsoft.Extensions.DependencyInjection;
using System.Windows;

namespace LiteObservableLogs.Demo.WPF;

public partial class MainWindow : Window
{
    public MainWindow()
    {
        InitializeComponent();
        DataContext = App.Host.Services.GetRequiredService<MainViewModel>();
        Closed += (_, _) => ((IDisposable)DataContext).Dispose();
    }
}
