using HattrickTransfersScraper.Models;
using Microsoft.Extensions.Logging;
using Microsoft.Playwright;
using Newtonsoft.Json;
using System.Globalization;
using System.Runtime.InteropServices;
using System.Text.RegularExpressions;

namespace HattrickTransfersScraper
{
    internal static partial class Helpers
    {
        [GeneratedRegex(@"playerId=(\d+)", RegexOptions.Compiled)]
        internal static partial Regex PlayerIdRegex();

        [GeneratedRegex(@"Deadline\s*(\d{1,2}/\d{1,2}/\d{4} \d{1,2}:\d{2}(?::\d{2})? [AP]M)", RegexOptions.Compiled | RegexOptions.IgnoreCase)]
        internal static partial Regex DeadlineRegex();

        [GeneratedRegex(@"Timestamp\s+([0-9]{1,2}/[0-9]{1,2}/[0-9]{4}\s+[0-9]{1,2}:[0-9]{2}(?::[0-9]{2})?\s*(?:AM|PM)?)", RegexOptions.Compiled | RegexOptions.IgnoreCase)]
        internal static partial Regex TimestampRegex();

        [GeneratedRegex(@"\d[\d\s]*", RegexOptions.Compiled)]
        internal static partial Regex PriceRegex();

        internal static readonly Settings _settings = LoadFileData<Settings>(Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "settings.json"));

        internal static readonly OSPlatform _currentPlatform = RuntimeInformation.IsOSPlatform(OSPlatform.Windows)
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
        internal static void LogAndPrint(ILogger? logger, LogLevel logLevel, string errorMessage, params object?[] parameters)
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
        internal static string GetTodaysLogFilePath() =>
            GetTodaysFilePath($"{DateTime.Now:yyyyMMdd}.log");

        /// <summary>
        /// Gets today's processed players file path
        /// </summary>
        internal static string GetTodaysProcessedPlayersFilePath() =>
            GetTodaysFilePath($"{DateTime.Now:yyyyMMdd}processed.json");

        /// <summary>
        /// Gets today's deal players file path
        /// </summary>
        internal static string GetTodaysDealPlayersFilePath() =>
            GetTodaysFilePath($"{DateTime.Now:yyyyMMdd}deals.json");

        /// <summary>
        /// Combines base log directory with today's date and given file name
        /// </summary>
        internal static string GetTodaysFilePath(string fileName) =>
            Path.Combine(Path.GetDirectoryName(_settings.Logs.Paths.GetPath(_currentPlatform))!, fileName);

        /// <summary>
        /// Checks if a player ID has already been processed
        /// </summary>
        internal static bool IsPlayerIdProcessed(string playerId) =>
            (LoadFileData<ProcessedPlayers>(GetTodaysProcessedPlayersFilePath())).Ids?.Contains(playerId) ?? false;

        /// <summary>
        /// Marks a player ID as processed and appends it to the file
        /// </summary>
        internal static void MarkPlayerIdAsProcessed(string? playerId)
        {
            if (string.IsNullOrWhiteSpace(playerId))
                return;

            ProcessedPlayers processed = LoadFileData<ProcessedPlayers>(GetTodaysProcessedPlayersFilePath());
            processed.Ids ??= [];

            if (processed.Ids.Add(playerId))
                File.WriteAllText(GetTodaysProcessedPlayersFilePath(), JsonConvert.SerializeObject(processed, Formatting.Indented));
        }

        /// <summary>
        /// Parses deadline string into DateTime
        /// </summary>
        internal static DateTime? ParseDeadline(string deadlineText)
        {
            if (DateTime.TryParseExact(deadlineText, "M/d/yyyy h:mm:ss tt", CultureInfo.InvariantCulture, DateTimeStyles.None, out DateTime deadline) ||
                DateTime.TryParseExact(deadlineText, "M/d/yyyy h:mm tt", CultureInfo.InvariantCulture, DateTimeStyles.None, out deadline))
                return deadline;

            return null;
        }

        /// <summary>
        /// Parses log time from string
        /// </summary>
        internal static DateTime? ParseLogTime(string input) =>
            DateTime.TryParse(input, out DateTime datetime) ? datetime : null;

        /// <summary>
        /// Checks if a player ID is in today's deals file and removes it if found
        /// </summary>
        internal static void RemovePlayerFromDealsFile(string? playerId)
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
        internal static void AddPlayerToDealsFile(string? playerId, DateTime? deadline, int price, int medianValue)
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
        internal static void RemoveDealPlayersFromProcessedFile(ILogger? logger)
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
            {
                File.WriteAllText(processedFilePath, JsonConvert.SerializeObject(processedPlayers, Formatting.Indented));

                LogAndPrint(logger, LogLevel.Information, "Removed {0} deals players from processed file", dealPlayerIds.Count);
            }
        }

        /// <summary>
        /// Gets the highest bid or asking price of the player from their page
        /// </summary>
        internal static async Task<int> GetPlayerPriceAsync(IPage page, ILogger? logger)
        {
            ILocator askingPrice = page.Locator("#ctl00_ctl00_CPContent_CPMain_updBid p:has-text('Asking Price')");
            await RetryAssertionAsync(logger, Assertions.Expect(askingPrice).ToBeVisibleAsync());

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
                LogAndPrint(logger, LogLevel.Warning, "Could not determine price for player at {0}", page.Url);
                await page.WaitForLoadStateAsync(LoadState.DOMContentLoaded);
                return 0;
            }
        }

        /// <summary>
        /// Gets the deadline of the player from their page
        /// </summary>
        internal static async Task<DateTime?> GetPlayerDeadlineAsync(IPage page, ILogger? logger)
        {
            ILocator deadlineLocator = page.Locator("#ctl00_ctl00_CPContent_CPMain_updBid p:has-text('Deadline')");
            await RetryAssertionAsync(logger, Assertions.Expect(deadlineLocator).ToBeVisibleAsync());

            if (await deadlineLocator.CountAsync() == 1)
            {
                string deadlineText = await deadlineLocator.First.TextContentAsync() ?? string.Empty;
                string cleaned = deadlineText.Replace("Deadline:", string.Empty, StringComparison.OrdinalIgnoreCase).Trim();
                if (DateTime.TryParseExact(cleaned, "d-M-yyyy HH:mm", CultureInfo.InvariantCulture, DateTimeStyles.None, out DateTime parsedDeadline))
                    return parsedDeadline;
            }

            LogAndPrint(logger, LogLevel.Warning, "Could not determine deadline for player at {0}", page.Url);
            return null;
        }

        /// <summary>
        /// Deduplicates, cleans up expired deals and sorts the deals file by deadline
        /// </summary>
        internal static void DeduplicateCleanupAndSortDealsFile(ILogger? logger)
        {
            string filePath = GetTodaysDealPlayersFilePath();

            string[] json = File.ReadAllLines(filePath);
            DealPlayers dealPlayers = JsonConvert.DeserializeObject<DealPlayers>(string.Join(Environment.NewLine, json)) ?? new();

            Dictionary<string, (DateTime deadline, DateTime timestamp, string info)> playersMap = [];

            foreach (string playerInfo in dealPlayers.Info)
            {
                Match playerIdMatch = PlayerIdRegex().Match(playerInfo);
                Match deadlineMatch = DeadlineRegex().Match(playerInfo);
                Match timestampMatch = TimestampRegex().Match(playerInfo);

                if (playerIdMatch.Success && deadlineMatch.Success && timestampMatch.Success)
                {
                    string playerId = playerIdMatch.Groups[1].Value;
                    DateTime? deadline = ParseDeadline(deadlineMatch.Groups[1].Value);
                    DateTime? timestamp = ParseLogTime(timestampMatch.Groups[1].Value);

                    if (deadline.HasValue && timestamp.HasValue && deadline.Value > DateTime.Now)
                        if (!playersMap.TryGetValue(playerId, out (DateTime deadline, DateTime timestamp, string info) existing) || timestamp.Value > existing.timestamp)
                            playersMap[playerId] = (deadline.Value, timestamp.Value, playerInfo);
                }
            }

            HashSet<string> sortedPlayersInfo = [.. playersMap.Values.OrderBy(x => x.deadline).Select(x => x.info)];

            File.WriteAllText(filePath, JsonConvert.SerializeObject(new DealPlayers { Info = sortedPlayersInfo }, Formatting.Indented));

            LogAndPrint(logger, LogLevel.Information, "Deduplicated and cleaned up deals file, removed {0}, kept {1}",
                dealPlayers.Info.Count - sortedPlayersInfo.Count, sortedPlayersInfo.Count);

            RemoveDealPlayersFromProcessedFile(logger);
        }

        /// <summary>
        /// Retries Playwright goto action
        /// </summary>
        internal static Task RetryGotoAsync(ILogger? logger, IPage page, string url, WaitUntilState waitUntil, int maxAttempts = 9, int delayBetweenAttempts = 1000) =>
            RetryPlaywrightActionAsync(logger, () => page.GotoAsync(url, new PageGotoOptions { WaitUntil = waitUntil }), $"Navigate to '{url}'", maxAttempts, delayBetweenAttempts);

        /// <summary>
        /// Retries Playwright fill action
        /// </summary>
        internal static Task RetryFillAsync(ILogger? logger, ILocator locator, string text, int maxAttempts = 9, int delayBetweenAttempts = 1000) =>
            RetryPlaywrightActionAsync(logger, () => locator.FillAsync(text), $"Fill locator with '{text}'", maxAttempts, delayBetweenAttempts);

        /// <summary>
        /// Retries Playwright select action
        /// </summary>
        internal static Task RetrySelectAsync(ILogger? logger, ILocator locator, string value, int maxAttempts = 9, int delayBetweenAttempts = 1000) =>
            RetryPlaywrightActionAsync(logger, () => locator.SelectOptionAsync(value), $"Select '{value}' on locator", maxAttempts, delayBetweenAttempts);

        /// <summary>
        /// Retries Playwright click action
        /// </summary>
        internal static Task RetryClickAsync(ILogger? logger, ILocator locator, int maxAttempts = 9, int delayBetweenAttempts = 1000) =>
            RetryPlaywrightActionAsync(logger, () => locator.ClickAsync(), "Click", maxAttempts, delayBetweenAttempts);

        /// <summary>
        /// Retries Playwright assertion action
        /// </summary>
        internal static Task RetryAssertionAsync(ILogger? logger, Task assertion, int maxAttempts = 9, int delayBetweenAttempts = 1000) =>
            RetryPlaywrightActionAsync(logger, () => assertion, "Assertion", maxAttempts, delayBetweenAttempts);

        /// <summary>
        /// Retries Playwright press action
        /// </summary>
        internal static Task RetryPressAsync(ILogger? logger, ILocator locator, string key, int maxAttempts = 9, int delayBetweenAttempts = 1000) =>
            RetryPlaywrightActionAsync(logger, () => locator.PressAsync(key), $"Press '{key}' on locator", maxAttempts, delayBetweenAttempts);

        /// <summary>
        /// Retries Playwright function
        /// </summary>
        internal static Task RetryFunctionAsync(ILogger? logger, Func<Task> function, string functionName, int maxAttempts = 9, int delayBetweenAttempts = 1000) =>
            RetryPlaywrightActionAsync(logger, function, functionName, maxAttempts, delayBetweenAttempts);

        /// <summary>
        /// Retries failed Playwright action with max attempts and delay between attempts
        /// </summary>
        private static async Task RetryPlaywrightActionAsync(ILogger? logger, Func<Task> action, string actionName, int maxAttempts, int delayBetweenAttempts)
        {
            for (int attempt = 1; attempt <= maxAttempts; attempt++)
            {
                try
                {
                    await action();
                    return;
                }
                catch (PlaywrightException ex)
                {
                    LogAndPrint(logger, LogLevel.Warning, $"{actionName} failed (attempt {attempt}/{maxAttempts})");

                    if (attempt > 3)
                        LogAndPrint(logger, LogLevel.Warning, "Error: {0}", ex.Message);

                    if (attempt == maxAttempts)
                        throw;

                    await Task.Delay(delayBetweenAttempts);
                }
            }
        }
    }
}
