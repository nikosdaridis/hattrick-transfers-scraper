using Microsoft.Extensions.Logging;
using Microsoft.Playwright;
using Newtonsoft.Json;
using System.Reflection;
using System.Text.RegularExpressions;
using System.Web;
using static HattrickTransfersScraper.Models.SearchFilters;

namespace HattrickTransfersScraper
{
    internal class HattrickService(ILogger<HattrickService> logger)
    {
        /// <summary>
        /// Logs into Hattrick and returns the logged-in page
        /// </summary>
        internal async Task<(IPage page, string subdomain)> LoginHattrickAsync(IBrowser browser, string loginName, string password)
        {
            IBrowserContext context = await browser.NewContextAsync(new BrowserNewContextOptions
            {
                ViewportSize = new ViewportSize
                {
                    Width = Random.Shared.Next(1200, 1920),
                    Height = Random.Shared.Next(800, 1080)
                },
                Locale = "en-US"
            });

            IPage page = await context.NewPageAsync();
            await Helpers.RetryGotoAsync(logger, page, "https://hattrick.org/", WaitUntilState.NetworkIdle);

            ILocator cookiesRejectButton = page.Locator("button[data-cky-tag='reject-button']");
            await Helpers.RetryAssertionAsync(logger, Assertions.Expect(cookiesRejectButton).ToBeVisibleAsync());
            await Helpers.RetryClickAsync(logger, cookiesRejectButton);

            ILocator loginButton = page.Locator("div.landing-form.presign-up p.extra-message a:has-text('Log In')");
            await Helpers.RetryAssertionAsync(logger, Assertions.Expect(loginButton).ToBeVisibleAsync());
            await Helpers.RetryClickAsync(logger, loginButton);

            ILocator usernameInput = page.Locator("input[id='inputLoginname']");
            await Helpers.RetryAssertionAsync(logger, Assertions.Expect(usernameInput).ToBeVisibleAsync());
            await Helpers.RetryFillAsync(logger, usernameInput, loginName);
            await Helpers.RetryFillAsync(logger, page.Locator("input[id='inputPassword']"), password);
            await Helpers.RetryPressAsync(logger, page.Locator("input[id='inputPassword']"), "Enter");

            ILocator myOfficeText = page.Locator("div.boxHead a:has-text('My Club')");
            await Helpers.RetryAssertionAsync(logger, Assertions.Expect(myOfficeText).ToBeVisibleAsync());

            Helpers.LogAndPrint(logger, LogLevel.Information, "Logged in as {0}", loginName);

            return (page, page.Url.Split('.')[0].Replace("https://", ""));
        }

        /// <summary>
        /// Applies the given filter and performs search on the transfers page
        /// </summary>
        internal async Task ApplyFilterAndSearchAsync(IPage page, string subdomain, SearchFilter filter)
        {
            await Helpers.RetryGotoAsync(logger, page, $"https://{subdomain}.hattrick.org/World/Transfers/", WaitUntilState.DOMContentLoaded);

            ILocator clearFilterButton = page.Locator("a[id='ctl00_ctl00_CPContent_CPMain_butClear']");
            await Helpers.RetryAssertionAsync(logger, Assertions.Expect(clearFilterButton).ToBeVisibleAsync());
            await Helpers.RetryClickAsync(logger, clearFilterButton);
            await page.WaitForLoadStateAsync(LoadState.DOMContentLoaded);

            ILocator skill4Selector = page.Locator("#ctl00_ctl00_CPContent_CPMain_ddlSkill4");
            await Helpers.RetryAssertionAsync(logger, Assertions.Expect(skill4Selector).ToBeVisibleAsync());

            foreach (PropertyInfo property in typeof(SearchFilter).GetProperties(BindingFlags.Public | BindingFlags.Instance))
            {
                string? value = property.GetValue(filter) as string;
                if (string.IsNullOrWhiteSpace(value))
                    continue;

                LocatorAttribute? locatorAttribute = property.GetCustomAttribute<LocatorAttribute>();
                if (locatorAttribute is null)
                    continue;

                ILocator locatorElement = page.Locator(locatorAttribute.Locator);
                await Helpers.RetryAssertionAsync(logger, Assertions.Expect(locatorElement).ToBeVisibleAsync());

                await (locatorAttribute.Locator switch
                {
                    string s when s.StartsWith("select") => Helpers.RetrySelectAsync(logger, locatorElement, value),
                    string s when s.StartsWith("input") => Helpers.RetryFillAsync(logger, locatorElement, value),
                    _ => Task.CompletedTask
                });

                await page.WaitForTimeoutAsync(Random.Shared.Next(200, 400));
            }

            await page.WaitForTimeoutAsync(Random.Shared.Next(200, 400));
            ILocator searchButton = page.Locator("#ctl00_ctl00_CPContent_CPMain_butSearch");
            await Helpers.RetryAssertionAsync(logger, Assertions.Expect(searchButton).ToBeVisibleAsync());
            await Helpers.RetryClickAsync(logger, searchButton);

            Helpers.LogAndPrint(logger, LogLevel.Information, "Searching filter: {0}", JsonConvert.SerializeObject(filter, Formatting.None, new JsonSerializerSettings() { NullValueHandling = NullValueHandling.Ignore }));
        }

        /// <summary>
        /// Collects links of players whose transfer deadline is today and not yet processed
        /// </summary>
        internal async Task<HashSet<string>> CollectTodayPlayersLinksAsync(IPage page, SearchFilter filter)
        {
            HashSet<string> playerLinks = [];

            await CollectFromCurrentPage(1);

            IReadOnlyList<ILocator> pageNumbers = await page.Locator("#ctl00_ctl00_CPContent_CPMain_ucPager_divWrapper a.page[href]").AllAsync();
            foreach (ILocator pageLink in pageNumbers)
            {
                string pageNumberText = (await pageLink.TextContentAsync())?.Trim() ?? "-1";

                if (!int.TryParse(pageNumberText, out int pageNumber))
                    continue;

                ILocator pagerLocator = page.Locator("div.PagerRight_Default").First;

                await Helpers.RetryFunctionAsync(logger,
                    async () =>
                    {
                        await pageLink.ClickAsync();
                        await Assertions.Expect(pagerLocator).ToHaveTextAsync(new Regex(@$"Displaying page {pageNumber} of \d+"), new() { Timeout = 2000 });
                    },
                    $"Click page link and wait for Displaying page {pageNumber} of \\d+");

                await CollectFromCurrentPage(pageNumber);
            }

            Helpers.LogAndPrint(logger, LogLevel.Information, "Processing {0} players", playerLinks.Count);

            return playerLinks;

            /// <summary>
            /// Collects player links from the current page
            /// </summary>
            async Task CollectFromCurrentPage(int? pageNumber)
            {
                await Helpers.RetryAssertionAsync(logger, Assertions.Expect(page.Locator("#ctl00_ctl00_CPContent_CPMain_ucPager_divWrapper")).ToBeVisibleAsync());

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

                    if (!string.IsNullOrWhiteSpace(deadlineText) && deadlineText.Contains("today", StringComparison.OrdinalIgnoreCase))
                        if (playerLinks.Add(href))
                            collectedPlayersCount++;
                }

                Helpers.LogAndPrint(logger, LogLevel.Information, "Found {0} players and collected {1} players from page {2}", playersInfo.Count, collectedPlayersCount, pageNumber);
            }
        }

        /// <summary>
        /// Processes a player: gets price, deadline, median value and decides if it's a deal
        /// </summary>
        internal async Task ProcessPlayerAsync(IPage page, string subdomain, string playerLink, ILogger<Program> logger)
        {
            await Helpers.RetryGotoAsync(logger, page, $"https://{subdomain}.hattrick.org/{playerLink}", WaitUntilState.DOMContentLoaded);

            string query = playerLink[(playerLink.IndexOf('?') + 1)..];
            string? playerId = HttpUtility.ParseQueryString(query)["playerId"];
            Helpers.MarkPlayerIdAsProcessed(playerId);

            ILocator injuryIcon = page.Locator("i.icon-injury");
            if (await injuryIcon.CountAsync() > 0)
                return;

            int price = await Helpers.GetPlayerPriceAsync(page, logger);
            DateTime? deadline = await Helpers.GetPlayerDeadlineAsync(page, logger);

            ILocator transferCompareButton = page.Locator("text=Transfer Compare");
            await Helpers.RetryAssertionAsync(logger, Assertions.Expect(transferCompareButton).ToBeVisibleAsync());
            await Helpers.RetryClickAsync(logger, transferCompareButton);
            await page.WaitForLoadStateAsync(LoadState.DOMContentLoaded);

            if (await page.Locator("tr:has(th:text('Median')) th.transfer-compare-bid").CountAsync() != 1)
            {
                await page.WaitForLoadStateAsync(LoadState.DOMContentLoaded);
                return;
            }

            string medianValueText = await page.Locator("tr:has(th:text('Median')) th.transfer-compare-bid").TextContentAsync() ?? string.Empty;
            int medianValue = int.Parse(new string([.. Helpers.PriceRegex().Match(medianValueText).Value.Where(char.IsDigit)]));

            double scaleFactor = medianValue switch
            {
                < 20000 => 10,
                < 50000 => 4,
                < 100000 => 3,
                < 500000 => 2,
                _ => 1.5
            };

            if (medianValue > 20000 && price * scaleFactor < medianValue)
                Helpers.AddPlayerToDealsFile(playerId, deadline, price, medianValue);
            else
                Helpers.RemovePlayerFromDealsFile(playerId);
        }
    }
}
