using System.Windows;
using Microsoft.Extensions.DependencyInjection;

namespace LiteObservableLogs.Demo.WPF;

public partial class MainWindow : Window
{
    public MainWindow()
    {
        InitializeComponent();
        MainViewModel vm = App.Host.Services.GetRequiredService<MainViewModel>();
        DataContext = vm;
        Closed += (_, _) => vm.Dispose();
    }
}
