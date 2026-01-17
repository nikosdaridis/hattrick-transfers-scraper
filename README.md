# Hattrick Transfers Scraper

![License](https://img.shields.io/badge/license-MIT-blue.svg)
![.NET](https://img.shields.io/badge/.NET-10-purple.svg)

A .NET 10 console application that searches for profitable player deals on [Hattrick](https://www.hattrick.org). The application logs into Hattrick, applies configurable search filters, analyzes player value versus asking price, and tracks profitable deals.

## About

Hattrick Transfers Scraper automates the process of finding undervalued players in the Hattrick transfer market. By configuring search filters and deal rules, you can automatically scan for players whose asking price is significantly below their median market value.

## How It Works

1. **Browser Launch** - Playwright launches Chromium
2. **Login** - Automated login to Hattrick with credentials from `settings.json`
3. **Filter Application** - Dynamically apply search filters via `LocatorAttribute` mapping
4. **Data Extraction** - Parses player information from transfer listing pages
5. **Profit Calculation** - Compares median market value plus weekly wage against asking price using tiered rules
6. **Deal Management** - Tracks qualifying deals and removes expired ones
7. **Logging** - Logging to aid debugging and monitoring

## Features

- **Configurable Search Filters** - Define multiple search criteria with 40+ filter options
- **Tiered Profit Analysis** - Adjustable profit factors based on player value tiers
- **Skill-Based Filtering** - Filter by different skills with min/max ranges
- **Specialty Filters** - Target players with specific specialties (Technical, Quick, Powerful, etc.)
- **Deadline Window** - Only scan players with transfers ending within a configurable time window
- **Deal Tracking** - Automatically tracks profitable deals and manages expired ones
- **Comprehensive Logging** - Serilog-based structured logging with OS-specific paths
- **Retry Logic** - Resilient web scraping with automatic retry on failures

## Requirements

- **.NET 10 Runtime**
- **Hattrick Account**
- **Internet Connection**

## Installation

- **Clone the repository**
- **Install Playwright chromium browser**
- **Run project**

## Configuration

Two configuration files will be created in the project root directory:

### 1. settings.json

```json
{
  "LoginName": "your_username",
  "LoginPassword": "your_password",
  "DateFormatOption": "DayMonthYear",
  "DeadlineWindowHours": 12,
  "MinimumMedianForDeal": 40000,
  "DealRules": [
    {
      "UpperMedianLimit": 100000,
      "ProfitFactor": 3.0
    },
    {
      "UpperMedianLimit": 500000,
      "ProfitFactor": 2.0
    },
    {
      "UpperMedianLimit": null,
      "ProfitFactor": 1.5
    }
  ],
  "Logs": {
    "Paths": {
      "Windows": "C:\\logs\\hattrick-transfers-scraper\\.log",
      "Linux": "/var/log/hattrick-transfers-scraper/.log"
    },
    "MinimumLevel": "Information",
    "FileSizeLimitBytes": 209715200,
    "RollingInterval": "Day",
    "RollOnFileSizeLimit": true,
    "Shared": true,
    "FlushToDiskIntervalSeconds": 1,
    "RetainedFileCountLimit": null,
    "FormatProviderCulture": "en-US"
  }
}
```

#### Settings Properties

| Property               | Type   | Description                                                        |
| ---------------------- | ------ | ------------------------------------------------------------------ |
| `LoginName`            | string | Your Hattrick username                                             |
| `LoginPassword`        | string | Your Hattrick password                                             |
| `DateFormatOption`     | string | Date format: `"DayMonthYear"` or `"MonthDayYear"`                  |
| `DeadlineWindowHours`  | int    | Only consider players with transfers ending within this many hours |
| `MinimumMedianForDeal` | int    | Minimum median value (in local currency) to qualify as a deal      |
| `DealRules`            | array  | Tiered profit rules (evaluated in order)                           |
| `Logs`                 | object | Serilog logging configuration                                      |

#### Deal Rules

Deal rules are evaluated in order. Each rule specifies:

- `UpperMedianLimit` - Maximum median value for this tier (null = unlimited)
- `ProfitFactor` - Required ratio: ((Median + Weekly Wage) / Asking Price) must exceed this value

**Example:**

- Median + Weekly Wage < 100,000: Must be 3x below asking price
- Median + Weekly Wage < 500,000: Must be 2x below asking price
- Median + Weekly Wage >= 500,000: Must be 1.5x below asking price

### 2. searchFilter.json

```json
{
  "Filters": [
    {
      "AgeMin": "21",
      "AgeDaysMin": "0",
      "AgeMax": "27",
      "AgeDaysMax": "111",
      "Skill1": "Keeper",
      "Skill1Min": "9",
      "Skill1Max": "12",
      "BidMax": "100000"
    },
    {
      "AgeMin": "27",
      "AgeDaysMin": "0",
      "AgeMax": "37",
      "AgeDaysMax": "111",
      "Skill1": "Defending",
      "Skill1Min": "13",
      "Skill1Max": "16",
      "BidMax": "100000"
    }
  ]
}
```

#### Available Filter Properties

| Property                                          | Type   | Description                                         |
| ------------------------------------------------- | ------ | --------------------------------------------------- |
| `AgeMin` / `AgeMax`                               | string | Age range in years                                  |
| `AgeDaysMin` / `AgeDaysMax`                       | string | Additional days precision                           |
| `Skill1-4`                                        | string | Skill name to filter (see skills below)             |
| `Skill1-4Min` / `Skill1-4Max`                     | string | Skill level range (1-20)                            |
| `BidMax`                                          | string | Maximum asking price                                |
| `TSIMin` / `TSIMax`                               | string | TSI (Total Skill Index) range                       |
| `SalaryMin` / `SalaryMax`                         | string | Weekly salary range                                 |
| `BornIn`                                          | string | Country ID (see BornInCountry enum)                 |
| `Continent`                                       | string | Continent filter                                    |
| `TransferCompareAvgMin` / `TransferCompareAvgMax` | string | Compare to average transfer price                   |
| `TechnicalSpecialty`                              | string | Set to `"true"` to filter Technical specialty       |
| `QuickSpecialty`                                  | string | Set to `"true"` to filter Quick specialty           |
| `PowerfulSpecialty`                               | string | Set to `"true"` to filter Powerful specialty        |
| `UnpredictableSpecialty`                          | string | Set to `"true"` to filter Unpredictable specialty   |
| `HeadSpecialty`                                   | string | Set to `"true"` to filter Head specialty            |
| `ResilientSpecialty`                              | string | Set to `"true"` to filter Resilient specialty       |
| `SupportSpecialty`                                | string | Set to `"true"` to filter Support specialty         |
| `NoSpecialty`                                     | string | Set to `"true"` to filter players with no specialty |

#### Available Skills

| Skill        | Description       |
| ------------ | ----------------- |
| `Keeper`     | Goalkeeping skill |
| `Defending`  | Defensive skill   |
| `Playmaking` | Playmaking skill  |
| `Winger`     | Winger skill      |
| `Scoring`    | Scoring skill     |
| `Passing`    | Passing skill     |
| `SetPieces`  | Set pieces skill  |
| `Experience` | Experience level  |
| `Leadership` | Leadership skill  |

## Data Files

The application creates and manages two data files:

| File             | Location              | Purpose                         |
| ---------------- | --------------------- | ------------------------------- |
| `deals.json`     | `logs/deals.json`     | Current profitable deals        |
| `processed.json` | `logs/processed.json` | Previously processed player IDs |

## License

This project is licensed under the MIT License - see the [LICENSE](LICENSE) file for details.
