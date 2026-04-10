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
            string logFile = Path.Combine(logFolder, "observable_{Timestamp:yyyyMMdd}.log");

            Log.Logger = new LoggerConfiguration()
                .WriteTo.File(logFile,
                    outputTemplate:
                    "[{Timestamp:HH:mm:ss.fff}] [{Level:u3}] {SourceContext}{NewLine}{Message}{NewLine}{Exception}{NewLine}",
                    rollingInterval: RollingInterval.Day,
                    retainedFileCountLimit: 31,
                    retainedFileTimeLimit: TimeSpan.FromDays(21))
                .WriteTo.Console(outputTemplate:
                    "[{Timestamp:HH:mm:ss} {Level:u3}] {Message}{NewLine}{Exception}")
                .WriteTo.Event(outputTemplate: "[{Timestamp:HH:mm:ss.fff}] [{Level:u3}] {SourceContext}{NewLine}{Message}{NewLine}{Exception}{NewLine}")
                .MinimumLevel.Debug()
                .CreateLogger();

            services.AddLogging(c => c.AddSerilog());
            services.AddSingleton<MainViewModel>();
        })
        .Build();

    /// <summary>
    /// Generic host for dependency injection; resolves <see cref="MainViewModel"/> and <see cref="ILogger{T}"/> from <see cref="IServiceProvider"/>.
    /// </summary>
    public static IHost Host => _host;
}
