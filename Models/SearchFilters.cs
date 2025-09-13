namespace HattrickTransfersScraper.Models
{
    internal sealed class SearchFilters
    {
        public SearchFilter[] Filters { get; init; } =
             [
                new ()
                {
                    AgeMin = "21",
                    AgeDaysMin = "0",
                    AgeMax = "27",
                    AgeDaysMax = "111",
                    Skill1 = "1",
                    Skill1Min = "9",
                    Skill1Max = "12",
                    Skill2 = null,
                    Skill2Min = null,
                    Skill2Max = null,
                    Skill3 = null,
                    Skill3Min = null,
                    Skill3Max = null,
                    Skill4 = null,
                    Skill4Min = null,
                    Skill4Max = null,
                    BidMax = "100000",
                    BornIn = null,
                    Continent = null,
                    TSIMin = null,
                    TSIMax = null,
                    SalaryMin = null,
                    SalaryMax = null,
                    TransferCompareAvgMin = null,
                    TransferCompareAvgMax = null
                },
                new ()
                {
                    AgeMin = "27",
                    AgeDaysMin = "0",
                    AgeMax = "37",
                    AgeDaysMax = "111",
                    Skill1 = "3",
                    Skill1Min = "13",
                    Skill1Max = "16",
                    Skill2 = null,
                    Skill2Min = null,
                    Skill2Max = null,
                    Skill3 = null,
                    Skill3Min = null,
                    Skill3Max = null,
                    Skill4 = null,
                    Skill4Min = null,
                    Skill4Max = null,
                    BidMax = "100000",
                    BornIn = null,
                    Continent = null,
                    TSIMin = null,
                    TSIMax = null,
                    SalaryMin = null,
                    SalaryMax = null,
                    TransferCompareAvgMin = null,
                    TransferCompareAvgMax = null
                }
             ];

        internal sealed class SearchFilter
        {
            [Locator("select[id='ctl00_ctl00_CPContent_CPMain_ddlAgeMin']")]
            public string? AgeMin { get; init; }

            [Locator("select[id='ctl00_ctl00_CPContent_CPMain_ddlAgeDaysMin']")]
            public string? AgeDaysMin { get; init; }

            [Locator("select[id='ctl00_ctl00_CPContent_CPMain_ddlAgeMax']")]
            public string? AgeMax { get; init; }

            [Locator("select[id='ctl00_ctl00_CPContent_CPMain_ddlAgeDaysMax']")]
            public string? AgeDaysMax { get; init; }

            [Locator("select[id='ctl00_ctl00_CPContent_CPMain_ddlSkill1']")]
            public string? Skill1 { get; init; }

            [Locator("select[id='ctl00_ctl00_CPContent_CPMain_ddlSkill1Min']")]
            public string? Skill1Min { get; init; }

            [Locator("select[id='ctl00_ctl00_CPContent_CPMain_ddlSkill1Max']")]
            public string? Skill1Max { get; init; }

            [Locator("select[id='ctl00_ctl00_CPContent_CPMain_ddlSkill2']")]
            public string? Skill2 { get; init; }

            [Locator("select[id='ctl00_ctl00_CPContent_CPMain_ddlSkill2Min']")]
            public string? Skill2Min { get; init; }

            [Locator("select[id='ctl00_ctl00_CPContent_CPMain_ddlSkill2Max']")]
            public string? Skill2Max { get; init; }

            [Locator("select[id='ctl00_ctl00_CPContent_CPMain_ddlSkill3']")]
            public string? Skill3 { get; init; }

            [Locator("select[id='ctl00_ctl00_CPContent_CPMain_ddlSkill3Min']")]
            public string? Skill3Min { get; init; }

            [Locator("select[id='ctl00_ctl00_CPContent_CPMain_ddlSkill3Max']")]
            public string? Skill3Max { get; init; }

            [Locator("select[id='ctl00_ctl00_CPContent_CPMain_ddlSkill4']")]
            public string? Skill4 { get; init; }

            [Locator("select[id='ctl00_ctl00_CPContent_CPMain_ddlSkill4Min']")]
            public string? Skill4Min { get; init; }

            [Locator("select[id='ctl00_ctl00_CPContent_CPMain_ddlSkill4Max']")]
            public string? Skill4Max { get; init; }

            [Locator("input[id='ctl00_ctl00_CPContent_CPMain_txtBidMax']")]
            public string? BidMax { get; init; }

            [Locator("select[id='ctl00_ctl00_CPContent_CPMain_ddlBornIn']")]
            public string? BornIn { get; init; }

            [Locator("select[id='ctl00_ctl00_CPContent_CPMain_ddlContinent']")]
            public string? Continent { get; init; }

            [Locator("input[id='ctl00_ctl00_CPContent_CPMain_txtTSIMin_text']")]
            public string? TSIMin { get; init; }

            [Locator("input[id='ctl00_ctl00_CPContent_CPMain_txtTSIMax_text']")]
            public string? TSIMax { get; init; }

            [Locator("input[id='ctl00_ctl00_CPContent_CPMain_txtSalaryMin']")]
            public string? SalaryMin { get; init; }

            [Locator("input[id='ctl00_ctl00_CPContent_CPMain_txtSalaryMax']")]
            public string? SalaryMax { get; init; }

            [Locator("input[id='ctl00_ctl00_CPContent_CPMain_txtTransferCompareAvgMin']")]
            public string? TransferCompareAvgMin { get; init; }

            [Locator("input[id='ctl00_ctl00_CPContent_CPMain_txtTransferCompareAvgMax']")]
            public string? TransferCompareAvgMax { get; init; }
        }
    }
}
