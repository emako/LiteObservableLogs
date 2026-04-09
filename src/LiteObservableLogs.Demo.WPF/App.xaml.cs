using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using System.Configuration;
using System.Data;
using System.IO;
using System.Windows;

namespace LiteObservableLogs.Demo.WPF;

public partial class App : Application
{
    // The.NET Generic Host provides dependency injection, configuration, logging, and other services.
    // https://docs.microsoft.com/dotnet/core/extensions/generic-host
    // https://docs.microsoft.com/dotnet/core/extensions/dependency-injection
    // https://docs.microsoft.com/dotnet/core/extensions/configuration
    // https://docs.microsoft.com/dotnet/core/extensions/logging
    private static readonly IHost _host = Host.CreateDefaultBuilder()
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
                    "[{Timestamp:HH:mm:ss} {Level:u3}] {Message:lj}{NewLine}{Exception}")
                .WriteTo.Event() // you can subscribe an event log from Log.Received to it in the UI to get real-time log updates
                .MinimumLevel.Debug()
                .CreateLogger();

            services.AddLogging(c => c.AddSerilog());
        });
}
