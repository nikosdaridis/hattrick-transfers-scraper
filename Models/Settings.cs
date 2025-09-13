using System.Runtime.InteropServices;

namespace HattrickTransfersScraper.Models
{
    internal sealed class Settings
    {
        public string LoginName { get; init; } = string.Empty;
        public string LoginPassword { get; init; } = string.Empty;
        public SerilogConfig Logs { get; init; } = new();

        internal sealed class SerilogConfig
        {
            public SerilogPathsConfig Paths { get; init; } = new();
            public string MinimumLevel { get; init; } = "Information";
            public long FileSizeLimitBytes { get; init; } = 209715200;
            public string RollingInterval { get; init; } = "Day";
            public bool RollOnFileSizeLimit { get; init; } = true;
            public bool Shared { get; init; } = true;
            public int FlushToDiskIntervalSeconds { get; init; } = 1;
            public int? RetainedFileCountLimit { get; init; } = null;
            public string FormatProviderCulture { get; init; } = "en-US";

            internal sealed class SerilogPathsConfig
            {
                public string Windows { get; init; } = @"C:\logs\hattrick-transfers-scraper\.log";
                public string Linux { get; init; } = @"/var/log/hattrick-transfers-scraper/.log";

                internal string GetPath(OSPlatform osPlatform) =>
                    osPlatform.ToString() switch
                    {
                        "WINDOWS" => Windows,
                        "LINUX" => Linux,
                        _ => throw new PlatformNotSupportedException($"No log path configured for the platform {osPlatform}.")
                    };
            }
        }
    }
}