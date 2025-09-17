using HattrickTransfersScraper.Models;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Playwright;
using System.Globalization;
using static HattrickTransfersScraper.Models.SearchFilters;

namespace HattrickTransfersScraper
{
    internal class Program
    {
        static async Task Main()
        {
            Settings settings = Helpers.LoadFileData<Settings>(Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "settings.json"));
            SearchFilters searchFilters = Helpers.LoadFileData<SearchFilters>(Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "searchFilter.json"));
            _ = Helpers.LoadFileData<ProcessedPlayers>(Helpers.GetTodaysProcessedPlayersFilePath());
            _ = Helpers.LoadFileData<DealPlayers>(Helpers.GetTodaysDealPlayersFilePath());

            CultureInfo.DefaultThreadCurrentCulture = new(settings.Logs.FormatProviderCulture);
            CultureInfo.DefaultThreadCurrentUICulture = new(settings.Logs.FormatProviderCulture);

            ServiceCollection serviceCollection = new();
            serviceCollection.AddServices(settings);
            ServiceProvider serviceProvider = serviceCollection.BuildServiceProvider();
            ILogger<Program> logger = serviceProvider.GetRequiredService<ILogger<Program>>();
            HattrickService hattrickService = serviceProvider.GetRequiredService<HattrickService>();

            try
            {
                await using IBrowser browser = await (await Playwright.CreateAsync()).Chromium.LaunchAsync(new BrowserTypeLaunchOptions
                {
                    Headless = false,
                    Args = new[] {
                        "--disable-blink-features=AutomationControlled",
                        "--no-sandbox",
                        "--disable-infobars"
                    }
                });

                (IPage page, string subdomain) = await hattrickService.LoginHattrickAsync(browser, settings.LoginName, settings.LoginPassword);

                foreach (SearchFilter filter in searchFilters.Filters)
                {
                    await hattrickService.ApplyFilterAndSearchAsync(page, subdomain, filter);
                    HashSet<string> playersLinks = await hattrickService.CollectTodayPlayersLinksAsync(page, filter);

                    foreach (string playerLink in playersLinks)
                        await hattrickService.ProcessPlayerAsync(page, subdomain, playerLink, logger, settings);
                }

                await browser.CloseAsync();

                Helpers.DeduplicateCleanupAndSortDealsFile(logger);
            }
            catch (Exception ex)
            {
                Helpers.LogAndPrint(logger, LogLevel.Critical, "Fatal error: {0}", ex);
            }
        }
    }
}
