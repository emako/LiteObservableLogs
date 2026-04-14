using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using System.IO;
using System.Windows;

namespace LiteObservableLogs.Demo.WPF;

public partial class App : Application
{
    // The .NET Generic Host provides dependency injection, configuration, logging, and other services.
    // https://docs.microsoft.com/dotnet/core/extensions/generic-host
    // https://docs.microsoft.com/dotnet/core/extensions/dependency-injection
    // https://docs.microsoft.com/dotnet/core/extensions/configuration
    // https://docs.microsoft.com/dotnet/core/extensions/logging
    private static readonly IHost _host = Microsoft.Extensions.Hosting.Host.CreateDefaultBuilder()
        .ConfigureLogging(builder => { })
        .ConfigureServices((context, services) =>
        {
            string logFolder = Path.Combine(AppContext.BaseDirectory, "log");
            Directory.CreateDirectory(logFolder);
            string logFile = Path.Combine(logFolder, "observable_{Timestamp:yyyyMMdd}_{Count:d5}.log");

            Log.Logger = new LoggerConfiguration()
                .WriteTo.File(logFile,
                    rollingInterval: RollingInterval.Day,
                    retainedFileCountLimit: 31,
                    retainedFileTimeLimit: TimeSpan.FromDays(21),
                    rollingSize: 10240L) // 1024 KB = 1 MB
                .WriteTo.Console(target: ConsoleTarget.Debug)
                .ObserveTo.Event()
                .WriteTo.Option(outputTemplate: "{Timestamp:yyyy-MM-dd HH:mm:ss.fff}|{UserName}|{Level:u5}|{ThreadId:d3}|{CallerFileName}:{CallerLineNumber}|{CallerMemberName}|{Message}{NewLine}{StackFrames}")
                .Dispatcher.Async()
                .MinimumLevel.Debug()
                .CreateLogger();

            services.AddSingleton<MainViewModel>();
        })
        .Build();

    /// <summary>
    /// Generic host for dependency injection; resolves <see cref="MainViewModel"/> and <see cref="ILogger{T}"/> from <see cref="IServiceProvider"/>.
    /// </summary>
    public static IHost Host => _host;
}
