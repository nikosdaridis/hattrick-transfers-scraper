using Newtonsoft.Json.Converters;
using System.Runtime.InteropServices;
using System.Text.Json.Serialization;

namespace HattrickTransfersScraper.Models
{
    internal sealed class Settings
    {
        public string LoginName { get; init; } = string.Empty;
        public string LoginPassword { get; init; } = string.Empty;
        public DateFormat DateFormatOption { get; init; } = DateFormat.DayMonthYear;
        public int DeadlineWindowHours { get; init; } = 12;
        public int MinimumMedianForDeal { get; init; } = 40000;
        public DealRule[] DealRules { get; init; } =
            [
            new() { UpperMedianLimit  = 100000, ProfitFactor  = 3 },
            new() { UpperMedianLimit  = 500000, ProfitFactor  = 2 },
            new() { UpperMedianLimit  = null, ProfitFactor  = 1.5 }
            ];

        public SerilogConfig Logs { get; init; } = new();

        [JsonConverter(typeof(StringEnumConverter))]
        public enum DateFormat
        {
            DayMonthYear,
            MonthDayYear
        }

        internal sealed class DealRule
        {
            public int? UpperMedianLimit { get; init; }
            public double ProfitFactor { get; init; }
        }

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