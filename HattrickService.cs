using HattrickTransfersScraper.Models;
using Microsoft.Extensions.Logging;
using Microsoft.Playwright;
using Newtonsoft.Json;
using Newtonsoft.Json.Converters;
using System.Reflection;
using System.Text.RegularExpressions;
using System.Web;
using static HattrickTransfersScraper.Models.SearchFilters;

namespace HattrickTransfersScraper
{
    internal class HattrickService(ILogger<HattrickService> logger)
    {
        private static readonly JsonSerializerSettings _logSerializerSettings = new()
        {
            NullValueHandling = NullValueHandling.Ignore,
            Converters = { new StringEnumConverter() }
        };

        /// <summary>
        /// Launches a new browser instance
        /// </summary>
        internal async Task<IBrowser> LaunchBrowserAsync() =>
            await (await Playwright.CreateAsync()).Chromium.LaunchAsync(new BrowserTypeLaunchOptions
            {
                Headless = false,
                Args = new[]
                {
                    "--disable-blink-features=AutomationControlled",
                    "--no-sandbox",
                    "--disable-infobars"
                }
            });

        /// <summary>
        /// Creates a new page with random viewport size
        /// </summary>
        internal async Task<IPage> CreatePageAsync(IBrowser browser) =>
           await (await browser.NewContextAsync(new BrowserNewContextOptions
           {
               ViewportSize = new ViewportSize
               {
                   Width = Random.Shared.Next(1200, 1400),
                   Height = Random.Shared.Next(1400, 1800)
               },
               Locale = "en-US"
           })).NewPageAsync();

        /// <summary>
        /// Logs in to Hattrick and returns the subdomain of the logged-in user
        /// </summary>
        internal async Task<string> LoginHattrickAsync(IPage page)
        {
            await Helpers.HandleGotoAsync(logger, page, "https://hattrick.org/", WaitUntilState.NetworkIdle);

            ILocator cookiesRejectButton = page.Locator("button[data-cky-tag='reject-button']");
            await Helpers.HandleAssertionAsync(logger, Assertions.Expect(cookiesRejectButton).ToBeVisibleAsync());
            await Helpers.HandleClickAsync(logger, cookiesRejectButton);

            ILocator loginButton = page.Locator("div.landing-form.presign-up p.extra-message a:has-text('Log In')");
            await Helpers.HandleAssertionAsync(logger, Assertions.Expect(loginButton).ToBeVisibleAsync());
            await Helpers.HandleClickAsync(logger, loginButton);

            ILocator usernameInput = page.Locator("input[id='inputLoginname']");
            await Helpers.HandleAssertionAsync(logger, Assertions.Expect(usernameInput).ToBeVisibleAsync());
            await Helpers.HandleFillAsync(logger, usernameInput, Helpers._settings.LoginName);
            await Helpers.HandleFillAsync(logger, page.Locator("input[id='inputPassword']"), Helpers._settings.LoginPassword);
            await Helpers.HandlePressAsync(logger, page.Locator("input[id='inputPassword']"), "Enter");

            ILocator myOfficeText = page.Locator("div.boxHead a:has-text('My Club')");
            await Helpers.HandleAssertionAsync(logger, Assertions.Expect(myOfficeText).ToBeVisibleAsync());

            Helpers.LogAndPrint(logger, LogLevel.Information, "Logged in as {0}", Helpers._settings.LoginName);

            return page.Url.Split('.')[0].Replace("https://", "");
        }

        /// <summary>
        /// Applies the given filter and performs search on the transfers page
        /// </summary>
        internal async Task ApplyFilterAndSearchAsync(IPage page, string subdomain, SearchFilter filter)
        {
            await Helpers.HandleGotoAsync(logger, page, $"https://{subdomain}.hattrick.org/World/Transfers/", WaitUntilState.DOMContentLoaded);
            await page.WaitForTimeoutAsync(Random.Shared.Next(200, 400));

            ILocator clearFilterButton = page.Locator("a[id='ctl00_ctl00_CPContent_CPMain_butClear']");
            await Helpers.HandleAssertionAsync(logger, Assertions.Expect(clearFilterButton).ToBeVisibleAsync());
            await Helpers.HandleClickAsync(logger, clearFilterButton);
            await page.WaitForLoadStateAsync(LoadState.DOMContentLoaded);

            await page.WaitForTimeoutAsync(Random.Shared.Next(200, 400));
            ILocator skill4Selector = page.Locator("#ctl00_ctl00_CPContent_CPMain_ddlSkill4");
            await Helpers.HandleAssertionAsync(logger, Assertions.Expect(skill4Selector).ToBeVisibleAsync());

            foreach (PropertyInfo property in typeof(SearchFilter).GetProperties(BindingFlags.Public | BindingFlags.Instance).Where(property => property.GetValue(filter) is not null))
            {
                LocatorAttribute? locatorAttribute = property.GetCustomAttribute<LocatorAttribute>();
                if (locatorAttribute is null)
                    continue;

                ILocator locatorElement = page.Locator(locatorAttribute.Locator);
                await Helpers.HandleAssertionAsync(logger, Assertions.Expect(locatorElement).ToBeVisibleAsync());

                Task actionTask = (property.GetValue(filter), property.PropertyType, locatorAttribute.Locator) switch
                {
                    // Clicks Specialty icon if true, supports bool
                    (bool value, Type type, string locator) when type == typeof(bool?) && value && locator.StartsWith("label:has(i") =>
                        Helpers.HandleClickAsync(logger, locatorElement),

                    // Selects value in dropdown, supports enums and strings
                    (object value, Type type, string locator) when locator.StartsWith("select") && !string.IsNullOrWhiteSpace(value?.GetStringValue()) =>
                        Helpers.HandleSelectAsync(logger, locatorElement, value.GetStringValue()!),

                    // Fills input field, supports strings
                    (string value, Type type, string locator) when type == typeof(string) && !string.IsNullOrWhiteSpace(value) && locator.StartsWith("input") =>
                        Helpers.HandleFillAsync(logger, locatorElement, value),

                    _ => Task.CompletedTask
                };

                await actionTask;

                await page.WaitForTimeoutAsync(Random.Shared.Next(200, 400));
            }

            ILocator searchButton = page.Locator("#ctl00_ctl00_CPContent_CPMain_butSearch");
            await Helpers.HandleAssertionAsync(logger, Assertions.Expect(searchButton).ToBeVisibleAsync());
            await Helpers.HandleClickAsync(logger, searchButton);

            Helpers.LogAndPrint(logger, LogLevel.Information, "Searching filter: {0}", JsonConvert.SerializeObject(filter, Formatting.None, _logSerializerSettings));
        }

        /// <summary>
        /// Collects links of players whose transfer deadline is within the specified window
        /// </summary>
        internal async Task<HashSet<string>> CollectPlayersLinksAsync(IPage page, SearchFilter filter)
        {
            HashSet<string> playerLinks = [];

            await Helpers.HandleAssertionAsync(logger, Assertions.Expect(page.Locator("#mainBody h1", new() { HasTextString = "Search Result" })).ToBeVisibleAsync());

            await CollectFromCurrentPageAsync(1);

            IReadOnlyList<ILocator> pageNumbers = await page.Locator("#ctl00_ctl00_CPContent_CPMain_ucPager_divWrapper a.page[href]").AllAsync();
            foreach (ILocator pageLink in pageNumbers)
            {
                string pageNumberText = (await pageLink.TextContentAsync())?.Trim() ?? "-1";

                if (!int.TryParse(pageNumberText, out int pageNumber))
                    continue;

                ILocator pagerLocator = page.Locator("div.PagerRight_Default").First;

                await Helpers.HandleFunctionAsync(logger,
                    async () =>
                    {
                        await pageLink.ClickAsync();
                        await Assertions.Expect(pagerLocator).ToHaveTextAsync(new Regex(@$"Displaying page {pageNumber} of"), new() { Timeout = 2000 });
                    },
                    $"Click page link and wait for 'Displaying page {pageNumber} of'");

                await CollectFromCurrentPageAsync(pageNumber);
            }

            Helpers.LogAndPrint(logger, LogLevel.Information, "Processing {0} players", playerLinks.Count);

            return playerLinks;

            /// <summary>
            /// Collects player links from the current page
            /// </summary>
            async Task CollectFromCurrentPageAsync(int? pageNumber)
            {
                if (await page.Locator("#ctl00_ctl00_CPContent_CPMain_ucPager_divWrapper").CountAsync() == 0)
                {
                    Helpers.LogAndPrint(logger, LogLevel.Warning, "No players found for filter: {0}", JsonConvert.SerializeObject(filter, Formatting.None, _logSerializerSettings));
                    return;
                }

                IReadOnlyList<ILocator> playersInfo = await page.Locator("div.transferPlayerInfo").AllAsync();
                int collectedPlayersCount = 0;

                foreach (ILocator playerInfo in playersInfo)
                {
                    ILocator linkLocator = playerInfo.Locator("h3.transfer_search_playername > a");
                    string? href = (await linkLocator.CountAsync()) > 0
                        ? await linkLocator.First.GetAttributeAsync("href")
                        : null;

                    string playerId = HttpUtility.ParseQueryString(href?[(href.IndexOf('?') + 1)..] ?? string.Empty)["playerId"] ?? string.Empty;

                    if (string.IsNullOrEmpty(href))
                        continue;

                    if (Helpers.IsPlayerIdProcessed(playerId))
                        continue;

                    ILocator parentFlex = playerInfo.Locator("xpath=ancestor::div[contains(@class,'flex')][1]");
                    ILocator deadlineLocator = parentFlex.Locator("span[id*='TransferPlayer_lblDeadline']");
                    string? deadlineText = (await deadlineLocator.CountAsync()) > 0
                        ? await deadlineLocator.First.TextContentAsync()
                        : null;

                    if (string.IsNullOrWhiteSpace(deadlineText))
                        continue;

                    DateTime? deadline = Helpers.ParseDeadline(deadlineText!, logger);
                    if (deadline is null)
                        continue;

                    double remainingHours = (deadline.Value - DateTime.Now).TotalHours;
                    if (remainingHours > 0 && remainingHours < Helpers._settings.DeadlineWindowHours)
                        if (playerLinks.Add(href))
                            collectedPlayersCount++;
                }

                Helpers.LogAndPrint(logger, LogLevel.Information, "Found {0} players and collected {1} players from page {2}", playersInfo.Count, collectedPlayersCount, pageNumber);
            }
        }

        /// <summary>
        /// Processes a player: gets weekly wage, deadline, price, median value and decides if it's a deal
        /// </summary>
        internal async Task ProcessPlayerAsync(IPage page, string subdomain, string playerLink, ILogger<Program> logger, Settings settings)
        {
            await Helpers.HandleGotoAsync(logger, page, $"https://{subdomain}.hattrick.org/{playerLink}", WaitUntilState.DOMContentLoaded);

            string query = playerLink[(playerLink.IndexOf('?') + 1)..];
            string? playerId = HttpUtility.ParseQueryString(query)["playerId"];
            Helpers.MarkPlayerIdAsProcessed(playerId);

            ILocator injuryIcon = page.Locator("i.icon-injury");
            if (await injuryIcon.CountAsync() > 0)
                return;

            int weeklyWage = await Helpers.GetWeeklyPlayerWageAsync(page, logger);
            DateTime? deadline = await Helpers.GetPlayerDeadlineAsync(page, logger);
            int price = await Helpers.GetPlayerPriceAsync(page, logger);
            int medianValue = await Helpers.GetMedianValueAsync(page, logger);

            double profitFactor = Helpers.GetProfitFactor(medianValue);

            if (medianValue > settings.MinimumMedianForDeal && (price + weeklyWage) * profitFactor < medianValue)
                Helpers.AddPlayerToDealsFile(playerId, weeklyWage, deadline, price, medianValue);
            else
                Helpers.RemovePlayerFromDealsFile(playerId);
        }
    }
}
