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
            ProcessedPlayers processedPlayers = Helpers.LoadFileData<ProcessedPlayers>(Helpers.GetTodaysProcessedPlayersFilePath());
            DealPlayers dealPlayers = Helpers.LoadFileData<DealPlayers>(Helpers.GetTodaysDealPlayersFilePath());

            CultureInfo.DefaultThreadCurrentCulture = new(settings.Logs.FormatProviderCulture);
            CultureInfo.DefaultThreadCurrentUICulture = new(settings.Logs.FormatProviderCulture);

            ServiceCollection serviceCollection = new();
            serviceCollection.AddServices(settings);
            ServiceProvider serviceProvider = serviceCollection.BuildServiceProvider();
            ILogger<Program> logger = serviceProvider.GetRequiredService<ILogger<Program>>();

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

                Helpers.RemoveDealPlayersFromProcessedFile(Helpers.GetTodaysDealPlayersFilePath());

                IPage page = await Helpers.LoginHattrickAsync(browser, settings.LoginName, settings.LoginPassword);
                string subdomain = page.Url.Split('.')[0].Replace("https://", "");

                foreach (SearchFilter filter in searchFilters.Filters)
                {
                    await Helpers.ApplyFilterAndSearchAsync(page, filter, subdomain);
                    HashSet<string> playersLinks = await Helpers.CollectTodayPlayersLinksAsync(page);

                    foreach (string playerLink in playersLinks)
                        await Helpers.ProcessPlayerAsync(page, subdomain, playerLink, logger);
                }

                await browser.CloseAsync();

                Helpers.DeduplicateCleanupAndSortDealsFile(Helpers.GetTodaysDealPlayersFilePath());
            }
            catch (Exception ex)
            {
                Helpers.LogAndPrint(logger, LogLevel.Critical, "Fatal error: {0}", ex);
            }
        }
    }
}
