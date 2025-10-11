using HattrickTransfersScraper.Models;
using Microsoft.Extensions.DependencyInjection;
using Serilog;
using Serilog.Events;
using System.Globalization;
using System.Runtime.InteropServices;

namespace HattrickTransfersScraper
{
    internal static class ConfigureServices
    {
        private static readonly OSPlatform[] _supportedOSPlatforms = [OSPlatform.Windows, OSPlatform.Linux];

        /// <summary>
        /// Adds services to the service collection
        /// </summary>
        internal static void AddServices(this IServiceCollection serviceCollection, Settings settings)
        {
            ConfigureSerilog(settings);
            serviceCollection.AddLogging(loggingBuilder => loggingBuilder.AddSerilog(dispose: true));

            serviceCollection.AddSingleton<HattrickService>();
        }

        /// <summary>
        /// Configures Serilog
        /// </summary>
        private static void ConfigureSerilog(Settings settings)
        {
            ArgumentNullException.ThrowIfNull(settings);

            Enum.TryParse(settings.Logs.MinimumLevel, true, out LogEventLevel minimumLevel);
            Enum.TryParse(settings.Logs.RollingInterval, true, out RollingInterval rollingInterval);

            Log.Logger = new LoggerConfiguration()
                .MinimumLevel.Is(minimumLevel)
                .WriteTo.File(
                    path: settings.Logs.Paths.GetPath(_supportedOSPlatforms.FirstOrDefault(RuntimeInformation.IsOSPlatform)),
                    fileSizeLimitBytes: settings.Logs.FileSizeLimitBytes,
                    rollingInterval: rollingInterval,
                    rollOnFileSizeLimit: settings.Logs.RollOnFileSizeLimit,
                    shared: settings.Logs.Shared,
                    flushToDiskInterval: TimeSpan.FromSeconds(settings.Logs.FlushToDiskIntervalSeconds),
                    retainedFileCountLimit: settings.Logs.RetainedFileCountLimit,
                    formatProvider: new CultureInfo(settings.Logs.FormatProviderCulture)
                )
                .CreateLogger();
        }
    }
}
