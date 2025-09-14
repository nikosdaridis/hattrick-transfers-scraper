using HattrickTransfersScraper.Models;
using Microsoft.Extensions.Logging;
using Microsoft.Playwright;
using Newtonsoft.Json;
using System.Globalization;
using System.Reflection;
using System.Runtime.InteropServices;
using System.Text.RegularExpressions;
using System.Web;
using static HattrickTransfersScraper.Models.SearchFilters;

namespace HattrickTransfersScraper
{
    internal static partial class Helpers
    {
        [GeneratedRegex(@"playerId=(\d+)", RegexOptions.Compiled)]
        private static partial Regex PlayerIdRegex();

        [GeneratedRegex(@"Deadline\s*(\d{1,2}/\d{1,2}/\d{4} \d{1,2}:\d{2}(?::\d{2})? [AP]M)", RegexOptions.Compiled | RegexOptions.IgnoreCase)]
        private static partial Regex DeadlineRegex();

        [GeneratedRegex(@"Timestamp\s+([0-9]{1,2}/[0-9]{1,2}/[0-9]{4}\s+[0-9]{1,2}:[0-9]{2}(?::[0-9]{2})?\s*(?:AM|PM)?)", RegexOptions.Compiled | RegexOptions.IgnoreCase)]
        private static partial Regex TimestampRegex();

        [GeneratedRegex(@"\d[\d\s]*", RegexOptions.Compiled)]
        private static partial Regex PriceRegex();

        private static readonly Settings _settings = LoadFileData<Settings>(Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "settings.json"));

        private static readonly OSPlatform _currentPlatform = RuntimeInformation.IsOSPlatform(OSPlatform.Windows)
               ? OSPlatform.Windows
               : OSPlatform.Linux;

        /// <summary>
        /// Loads data from file or backups current file and creates a default file
        /// </summary>
        internal static T LoadFileData<T>(string filePath, ILogger? logger = null) where T : new()
        {
            T defaultModel = new();

            try
            {
                string jsonContent = File.ReadAllText(filePath);

                if (string.IsNullOrEmpty(jsonContent))
                {
                    jsonContent = JsonConvert.SerializeObject(defaultModel, Formatting.Indented);
                    File.WriteAllText(filePath, jsonContent);
                    return defaultModel;
                }

                return JsonConvert.DeserializeObject<T>(jsonContent) ?? defaultModel;
            }
            catch (Exception ex)
            {
                LogAndPrint(logger, LogLevel.Error, "Invalid Json - Error reading file '{0}': {1}", filePath, ex.Message);

                if (File.Exists(filePath))
                {
                    try
                    {
                        string backupFilePath = Path.ChangeExtension(filePath, $".invalid.{DateTime.Now:MMddHHmmss}.json");

                        File.Move(filePath, backupFilePath);
                        LogAndPrint(logger, LogLevel.Warning, "Existing file backed up as {0}", backupFilePath);
                    }
                    catch (Exception backupEx)
                    {
                        LogAndPrint(logger, LogLevel.Error, "Failed to back up the existing file: {0}", backupEx.Message);
                    }
                }

                string defaultJson = JsonConvert.SerializeObject(defaultModel, Formatting.Indented);
                File.WriteAllText(filePath, defaultJson);
                return defaultModel;
            }
        }

        /// <summary>
        /// Logs and prints message
        /// </summary>
        public static void LogAndPrint(ILogger? logger, LogLevel logLevel, string errorMessage, params object?[] parameters)
        {
            string formattedMessage = string.Format(errorMessage, parameters);

            logger?.Log(logLevel, formattedMessage);

            Console.ForegroundColor = logLevel switch
            {
                LogLevel.Critical => ConsoleColor.DarkMagenta,
                LogLevel.Error => ConsoleColor.DarkRed,
                LogLevel.Warning => ConsoleColor.DarkYellow,
                LogLevel.Information => ConsoleColor.DarkCyan,
                LogLevel.Debug => ConsoleColor.DarkGreen,
                LogLevel.Trace => ConsoleColor.DarkGray,
                _ => ConsoleColor.White
            };

            Console.WriteLine(formattedMessage);
        }

        /// <summary>
        /// Gets today's log file path
        /// </summary>
        public static string GetTodaysLogFilePath() =>
            GetTodaysFilePath($"{DateTime.Now:yyyyMMdd}.log");

        /// <summary>
        /// Gets today's processed players file path
        /// </summary>
        public static string GetTodaysProcessedPlayersFilePath() =>
            GetTodaysFilePath($"{DateTime.Now:yyyyMMdd}processed.json");

        /// <summary>
        /// Gets today's deal players file path
        /// </summary>
        public static string GetTodaysDealPlayersFilePath() =>
            GetTodaysFilePath($"{DateTime.Now:yyyyMMdd}deals.json");

        /// <summary>
        /// Combines base log directory with today's date and given file name
        /// </summary>
        private static string GetTodaysFilePath(string fileName) =>
            Path.Combine(Path.GetDirectoryName(_settings.Logs.Paths.GetPath(_currentPlatform))!, fileName);

        /// <summary>
        /// Checks if a player ID has already been processed
        /// </summary>
        public static bool IsPlayerIdProcessed(string playerId) =>
            (LoadFileData<ProcessedPlayers>(GetTodaysProcessedPlayersFilePath())).Ids?.Contains(playerId) ?? false;

        /// <summary>
        /// Marks a player ID as processed and appends it to the file
        /// </summary>
        public static void MarkPlayerIdAsProcessed(string? playerId)
        {
            if (string.IsNullOrWhiteSpace(playerId))
                return;

            ProcessedPlayers processed = LoadFileData<ProcessedPlayers>(GetTodaysProcessedPlayersFilePath());
            processed.Ids ??= [];

            if (processed.Ids.Add(playerId))
                File.WriteAllText(GetTodaysProcessedPlayersFilePath(), JsonConvert.SerializeObject(processed, Formatting.Indented));
        }

        /// <summary>
        /// Logs into Hattrick and returns the logged-in page
        /// </summary>
        public static async Task<IPage> LoginHattrickAsync(IBrowser browser, string loginName, string password)
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
            await page.GotoAsync("https://hattrick.org/", new() { WaitUntil = WaitUntilState.NetworkIdle });

            ILocator cookiesRejectButton = page.Locator("button[data-cky-tag='reject-button']");
            await cookiesRejectButton.WaitForAsync(new() { State = WaitForSelectorState.Visible });
            await page.WaitForTimeoutAsync(Random.Shared.Next(200, 400));
            await cookiesRejectButton.ClickAsync();

            await page.ClickAsync("text=Log In");
            await page.FillAsync("input[id='inputLoginname']", loginName);
            await page.WaitForTimeoutAsync(Random.Shared.Next(200, 400));
            await page.FillAsync("input[id='inputPassword']", password);
            await page.WaitForTimeoutAsync(Random.Shared.Next(200, 400));
            await page.PressAsync("input[id='inputPassword']", "Enter");
            await page.WaitForTimeoutAsync(Random.Shared.Next(2000, 4000));

            return page;
        }

        /// <summary>
        /// Applies the given filter and performs search on the transfers page
        /// </summary>
        public static async Task ApplyFilterAndSearchAsync(IPage page, SearchFilter filter, string subdomain)
        {
            await page.GotoAsync($"https://{subdomain}.hattrick.org/World/Transfers/", new() { WaitUntil = WaitUntilState.DOMContentLoaded });
            await page.WaitForTimeoutAsync(Random.Shared.Next(200, 400));

            ILocator clearFilterButton = page.Locator("a[id='ctl00_ctl00_CPContent_CPMain_butClear']");
            await clearFilterButton.WaitForAsync(new() { State = WaitForSelectorState.Visible });
            await page.WaitForTimeoutAsync(Random.Shared.Next(200, 400));
            await clearFilterButton.ClickAsync();
            await page.WaitForTimeoutAsync(Random.Shared.Next(200, 400));

            foreach (PropertyInfo property in typeof(SearchFilter).GetProperties(BindingFlags.Public | BindingFlags.Instance))
            {
                string? value = property.GetValue(filter) as string;
                if (string.IsNullOrWhiteSpace(value))
                    continue;

                LocatorAttribute? locator = property.GetCustomAttribute<LocatorAttribute>();
                if (locator is null)
                    continue;

                ILocator locatorElement = page.Locator(locator.Locator);
                await locatorElement.WaitForAsync(new() { State = WaitForSelectorState.Visible });

                await (locator.Locator switch
                {
                    string s when s.StartsWith("select") => locatorElement.SelectOptionAsync(value),
                    string s when s.StartsWith("input") => locatorElement.FillAsync(value),
                    _ => Task.CompletedTask
                });

                await page.WaitForTimeoutAsync(Random.Shared.Next(200, 400));
            }

            ILocator searchButton = page.Locator("#ctl00_ctl00_CPContent_CPMain_butSearch");
            await searchButton.WaitForAsync(new() { State = WaitForSelectorState.Visible });
            await page.WaitForTimeoutAsync(Random.Shared.Next(200, 400));
            await searchButton.ClickAsync();
        }

        /// <summary>
        /// Collects players links with deadlines set to today from the current search results
        /// </summary>
        public static async Task<HashSet<string>> CollectTodayPlayersLinksAsync(IPage page)
        {
            HashSet<string> playerLinks = [];

            await CollectFromCurrentPage();

            IReadOnlyList<ILocator> pageNumbers = await page.Locator("#ctl00_ctl00_CPContent_CPMain_ucPager_divWrapper a.page").AllAsync();
            foreach (ILocator pageLink in pageNumbers)
            {
                string? isDisabled = await pageLink.GetAttributeAsync("disabled");
                if (!string.IsNullOrEmpty(isDisabled))
                    continue;

                await pageLink.ClickAsync();
                await page.WaitForLoadStateAsync(LoadState.DOMContentLoaded);
                await CollectFromCurrentPage();
            }

            return playerLinks;

            /// <summary>
            /// Collects player links from the current page
            /// </summary>
            async Task CollectFromCurrentPage()
            {
                await page.WaitForSelectorAsync("#ctl00_ctl00_CPContent_CPMain_ucPager2_divWrapper", new() { State = WaitForSelectorState.Visible });
                await page.WaitForTimeoutAsync(Random.Shared.Next(200, 400));

                IReadOnlyList<ILocator> playersInfo = await page.Locator("div.transferPlayerInfo").AllAsync();

                foreach (ILocator playerInfo in playersInfo)
                {
                    ILocator linkLocator = playerInfo.Locator("h3.transfer_search_playername > a");
                    string? href = (await linkLocator.CountAsync()) > 0
                        ? await linkLocator.First.GetAttributeAsync("href")
                        : null;

                    string playerId = HttpUtility.ParseQueryString(href?[(href.IndexOf('?') + 1)..] ?? string.Empty)["playerId"] ?? string.Empty;

                    if (string.IsNullOrEmpty(href))
                        continue;

                    if (IsPlayerIdProcessed(playerId))
                        continue;

                    ILocator parentFlex = playerInfo.Locator("xpath=ancestor::div[contains(@class,'flex')][1]");
                    ILocator deadlineLocator = parentFlex.Locator("span[id*='TransferPlayer_lblDeadline']");
                    string? deadlineText = (await deadlineLocator.CountAsync()) > 0
                        ? await deadlineLocator.First.TextContentAsync()
                        : null;

                    if (!string.IsNullOrWhiteSpace(deadlineText) && deadlineText.Contains("today", StringComparison.OrdinalIgnoreCase))
                        playerLinks.Add(href);
                }
            }
        }

        /// <summary>
        /// Processes a player: gets price, deadline, median value and decides if it's a deal
        /// </summary>
        public static async Task ProcessPlayerAsync(IPage page, string subdomain, string playerLink, ILogger<Program> logger)
        {
            await page.GotoAsync($"https://{subdomain}.hattrick.org/{playerLink}", new() { WaitUntil = WaitUntilState.DOMContentLoaded });
            await page.WaitForTimeoutAsync(Random.Shared.Next(200, 400));

            string query = playerLink[(playerLink.IndexOf('?') + 1)..];
            string? playerId = HttpUtility.ParseQueryString(query)["playerId"];
            MarkPlayerIdAsProcessed(playerId);

            ILocator injuryIcon = page.Locator("i.icon-injury");
            if (await injuryIcon.CountAsync() > 0)
                return;

            int price = await GetPlayerPriceAsync(page);
            DateTime? deadline = await GetPlayerDeadlineAsync(page);

            ILocator transferCompareButton = page.Locator("text=Transfer Compare");
            await transferCompareButton.WaitForAsync(new() { State = WaitForSelectorState.Visible });
            await transferCompareButton.ClickAsync();
            await page.WaitForLoadStateAsync(LoadState.DOMContentLoaded);

            if (await page.Locator("tr:has(th:text('Median')) th.transfer-compare-bid").CountAsync() != 1)
            {
                await page.WaitForLoadStateAsync(LoadState.DOMContentLoaded);
                await page.WaitForTimeoutAsync(Random.Shared.Next(200, 400));
                return;
            }

            string medianValueText = await page.Locator("tr:has(th:text('Median')) th.transfer-compare-bid").TextContentAsync() ?? string.Empty;
            int medianValue = int.Parse(new string([.. PriceRegex().Match(medianValueText).Value.Where(char.IsDigit)]));
            double scaleFactor = medianValue switch
            {
                < 20000 => 10,
                < 50000 => 4,
                < 100000 => 3,
                < 500000 => 2,
                _ => 1.5
            };

            if (medianValue > 20000 && price * scaleFactor < medianValue)
                AddPlayerToDealsFile(playerId, deadline, price, medianValue);
            else
                RemovePlayerFromDealsFile(playerId);
        }

        /// <summary>
        /// Gets the highest bid or asking price of the player from their page
        /// </summary>
        public static async Task<int> GetPlayerPriceAsync(IPage page)
        {
            ILocator askingPrice = page.Locator("#ctl00_ctl00_CPContent_CPMain_updBid p:has-text('Asking Price')");
            await askingPrice.WaitForAsync(new() { State = WaitForSelectorState.Visible });

            if (await page.Locator("#ctl00_ctl00_CPContent_CPMain_pnlHighestBid p").CountAsync() == 1)
            {
                string highestBidText = await page.Locator("#ctl00_ctl00_CPContent_CPMain_pnlHighestBid p").TextContentAsync() ?? string.Empty;
                return int.Parse(new string([.. PriceRegex().Match(highestBidText).Value.Where(char.IsDigit)]));
            }
            else if (await page.Locator("#ctl00_ctl00_CPContent_CPMain_updBid p:has-text('Asking Price')").CountAsync() == 1)
            {
                string askingPriceText = await page.Locator("#ctl00_ctl00_CPContent_CPMain_updBid p:has-text('Asking Price')").TextContentAsync() ?? "0";
                return int.Parse(new string([.. PriceRegex().Match(askingPriceText).Value.Where(char.IsDigit)]));
            }
            else
            {
                await page.WaitForLoadStateAsync(LoadState.DOMContentLoaded);
                await page.WaitForTimeoutAsync(Random.Shared.Next(200, 400));
                return 0;
            }
        }

        /// <summary>
        /// Gets the deadline of the player from their page
        /// </summary>
        public static async Task<DateTime?> GetPlayerDeadlineAsync(IPage page)
        {
            ILocator deadlineLocator = page.Locator("#ctl00_ctl00_CPContent_CPMain_updBid p:has-text('Deadline')");
            await deadlineLocator.WaitForAsync(new() { State = WaitForSelectorState.Visible });

            if (await deadlineLocator.CountAsync() == 1)
            {
                string deadlineText = await deadlineLocator.First.TextContentAsync() ?? string.Empty;
                string cleaned = deadlineText.Replace("Deadline:", string.Empty, StringComparison.OrdinalIgnoreCase).Trim();
                if (DateTime.TryParseExact(cleaned, "d-M-yyyy HH:mm", CultureInfo.InvariantCulture, DateTimeStyles.None, out DateTime parsedDeadline))
                    return parsedDeadline;
            }

            return null;
        }

        /// <summary>
        /// Deduplicates, cleans up expired deals and sorts the deals file by deadline
        /// </summary>
        public static void DeduplicateCleanupAndSortDealsFile()
        {
            string filePath = GetTodaysDealPlayersFilePath();

            if (!File.Exists(filePath))
                return;

            string[] json = File.ReadAllLines(filePath);
            DealPlayers dealPlayers = JsonConvert.DeserializeObject<DealPlayers>(string.Join(Environment.NewLine, json)) ?? new();

            Dictionary<string, (DateTime timestamp, DateTime deadline, string info)> playersMap = [];

            foreach (string playerInfo in dealPlayers.Info)
            {
                Match playerMatch = PlayerIdRegex().Match(playerInfo);
                Match deadlineMatch = DeadlineRegex().Match(playerInfo);
                Match timestampMatch = TimestampRegex().Match(playerInfo);

                if (playerMatch.Success && deadlineMatch.Success && timestampMatch.Success)
                {
                    string playerId = playerMatch.Groups[1].Value;
                    DateTime? deadline = ParseDeadline(deadlineMatch.Groups[1].Value);
                    DateTime? timestamp = ParseLogTime(timestampMatch.Groups[1].Value);

                    if (deadline.HasValue && timestamp.HasValue && deadline.Value > DateTime.Now)
                        if (!playersMap.TryGetValue(playerId, out (DateTime deadline, DateTime timestamp, string info) existing) || timestamp.Value > existing.timestamp)
                            playersMap[playerId] = (timestamp.Value, deadline.Value, playerInfo);
                }
            }

            HashSet<string> sortedPlayersInfo = [.. playersMap.Values.OrderBy(x => x.deadline).Select(x => x.info)];

            File.WriteAllText(filePath, JsonConvert.SerializeObject(new DealPlayers { Info = sortedPlayersInfo }, Formatting.Indented));

            RemoveDealPlayersFromProcessedFile();
        }

        /// <summary>
        /// Parses deadline string into DateTime
        /// </summary>
        public static DateTime? ParseDeadline(string deadlineText)
        {
            if (DateTime.TryParseExact(deadlineText, "M/d/yyyy h:mm:ss tt", CultureInfo.InvariantCulture, DateTimeStyles.None, out DateTime deadline) ||
                DateTime.TryParseExact(deadlineText, "M/d/yyyy h:mm tt", CultureInfo.InvariantCulture, DateTimeStyles.None, out deadline))
                return deadline;

            return null;
        }

        /// <summary>
        /// Parses log time from string
        /// </summary>
        public static DateTime? ParseLogTime(string input) =>
            DateTime.TryParse(input, out DateTime datetime) ? datetime : null;

        /// <summary>
        /// Checks if a player ID is in today's deals file and removes it if found
        /// </summary>
        public static void RemovePlayerFromDealsFile(string? playerId)
        {
            if (string.IsNullOrWhiteSpace(playerId))
                return;

            DealPlayers? dealPlayers = JsonConvert.DeserializeObject<DealPlayers>(File.ReadAllText(GetTodaysDealPlayersFilePath()));

            bool playerRemoved = dealPlayers?.Info.RemoveWhere(info => PlayerIdRegex().Match(info) is Match match && match.Success && match.Groups[1].Value == playerId) > 0;

            if (playerRemoved)
                File.WriteAllText(GetTodaysDealPlayersFilePath(), JsonConvert.SerializeObject(dealPlayers, Formatting.Indented));
        }

        /// <summary>
        /// Adds a player's deal info to today's deals file
        /// </summary>
        public static void AddPlayerToDealsFile(string? playerId, DateTime? deadline, int price, int medianValue)
        {
            if (string.IsNullOrWhiteSpace(playerId) || !deadline.HasValue)
                return;

            string filePath = GetTodaysDealPlayersFilePath();

            DealPlayers dealPlayers = LoadFileData<DealPlayers>(filePath);

            dealPlayers.Info.Add($"https://hattrick.org/goto.ashx?path=/Club/Players/Player.aspx?playerId={playerId} | Deadline {deadline} | Price: {price:N0} | Median: {medianValue:N0} | Timestamp {DateTime.Now}");

            File.WriteAllText(filePath, JsonConvert.SerializeObject(dealPlayers, Formatting.Indented));
        }

        /// <summary>
        /// Removes players from today's processed file if they are present in today's deals file
        /// </summary>
        public static void RemoveDealPlayersFromProcessedFile()
        {
            string dealsFilePath = GetTodaysDealPlayersFilePath();

            DealPlayers? dealPlayers = JsonConvert.DeserializeObject<DealPlayers>(File.ReadAllText(dealsFilePath));

            HashSet<string> dealPlayerIds = dealPlayers?.Info
                 .Select(info => PlayerIdRegex().Match(info))
                 .Where(match => match.Success)
                 .Select(match => match.Groups[1].Value)
                 .ToHashSet() ?? [];

            if (dealPlayerIds.Count == 0)
                return;

            string processedFilePath = GetTodaysProcessedPlayersFilePath();
            ProcessedPlayers? processedPlayers = JsonConvert.DeserializeObject<ProcessedPlayers>(File.ReadAllText(processedFilePath));

            bool anyRemoved = processedPlayers?.Ids?.RemoveWhere(id => dealPlayerIds.Contains(id)) > 0;

            if (anyRemoved)
                File.WriteAllText(processedFilePath, JsonConvert.SerializeObject(processedPlayers, Formatting.Indented));
        }
    }
}
