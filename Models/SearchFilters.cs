using Newtonsoft.Json.Converters;
using System.Text.Json.Serialization;

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
                    Skill1 = SkillType.Keeper,
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
                    Skill1 = SkillType.Defending,
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
            public SkillType? Skill1 { get; init; }

            [Locator("select[id='ctl00_ctl00_CPContent_CPMain_ddlSkill1Min']")]
            public string? Skill1Min { get; init; }

            [Locator("select[id='ctl00_ctl00_CPContent_CPMain_ddlSkill1Max']")]
            public string? Skill1Max { get; init; }

            [Locator("select[id='ctl00_ctl00_CPContent_CPMain_ddlSkill2']")]
            public SkillType? Skill2 { get; init; }

            [Locator("select[id='ctl00_ctl00_CPContent_CPMain_ddlSkill2Min']")]
            public string? Skill2Min { get; init; }

            [Locator("select[id='ctl00_ctl00_CPContent_CPMain_ddlSkill2Max']")]
            public string? Skill2Max { get; init; }

            [Locator("select[id='ctl00_ctl00_CPContent_CPMain_ddlSkill3']")]
            public SkillType? Skill3 { get; init; }

            [Locator("select[id='ctl00_ctl00_CPContent_CPMain_ddlSkill3Min']")]
            public string? Skill3Min { get; init; }

            [Locator("select[id='ctl00_ctl00_CPContent_CPMain_ddlSkill3Max']")]
            public string? Skill3Max { get; init; }

            [Locator("select[id='ctl00_ctl00_CPContent_CPMain_ddlSkill4']")]
            public SkillType? Skill4 { get; init; }

            [Locator("select[id='ctl00_ctl00_CPContent_CPMain_ddlSkill4Min']")]
            public string? Skill4Min { get; init; }

            [Locator("select[id='ctl00_ctl00_CPContent_CPMain_ddlSkill4Max']")]
            public string? Skill4Max { get; init; }

            [Locator("label:has(i.icon-speciality-1)")]
            public bool? TechnicalSpecialty { get; init; }

            [Locator("label:has(i.icon-speciality-2)")]
            public bool? QuickSpecialty { get; init; }

            [Locator("label:has(i.icon-speciality-3)")]
            public bool? PowerfulSpecialty { get; init; }

            [Locator("label:has(i.icon-speciality-4)")]
            public bool? UnpredictableSpecialty { get; init; }

            [Locator("label:has(i.icon-speciality-5)")]
            public bool? HeadSpecialty { get; init; }

            [Locator("label:has(i.icon-speciality-6)")]
            public bool? ResilientSpecialty { get; init; }

            [Locator("label:has(i.icon-speciality-8)")]
            public bool? SupportSpecialty { get; init; }

            [Locator("label:has(i.icon-speciality-0)")]
            public bool? NoSpecialty { get; init; }

            [Locator("input[id='ctl00_ctl00_CPContent_CPMain_txtBidMax']")]
            public string? BidMax { get; init; }

            [Locator("select[id='ctl00_ctl00_CPContent_CPMain_ddlBornIn']")]
            public BornInCountry? BornIn { get; init; }

            [Locator("select[id='ctl00_ctl00_CPContent_CPMain_ddlContinent']")]
            public ContinentType? Continent { get; init; }

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

    /// <summary>
    /// Extension method for types used in filters to get their string representation for setting filter values
    /// </summary>
    internal static class FilterExtensions
    {
        internal static string? GetStringValue(this object? propertyValue)
        {
            if (propertyValue is null)
                return null;

            if (propertyValue is string s)
                return s;

            if (propertyValue.GetType().IsEnum)
                return Convert.ToInt32(propertyValue).ToString();

            Type type = propertyValue.GetType();
            if (Nullable.GetUnderlyingType(type)?.IsEnum == true)
                return Convert.ToInt32(propertyValue).ToString();

            return propertyValue.ToString();
        }
    }

    [JsonConverter(typeof(StringEnumConverter))]
    internal enum SkillType
    {
        Keeper = 1,
        Defending = 3,
        Playmaking = 4,
        Winger = 5,
        Scoring = 6,
        Passing = 8,
        SetPieces = 7,
        Experience = 9,
        Leadership = 10
    }

    [JsonConverter(typeof(StringEnumConverter))]
    public enum BornInCountry
    {
        AnyCountry = -1,
        Albania = 94,
        Algeria = 126,
        Andorra = 101,
        Angola = 137,
        Argentina = 7,
        Armenia = 104,
        Austria = 33,
        Azerbaijan = 133,
        Bahamas = 199,
        Bahrain = 129,
        Bangladesh = 138,
        Barbados = 130,
        Belarus = 87,
        Belgium = 38,
        Belize = 186,
        Benin = 147,
        Bolivia = 69,
        BosniaAndHerzegovina = 63,
        Botswana = 185,
        Brazil = 22,
        Brunei = 143,
        Bulgaria = 55,
        BurkinaFaso = 193,
        Cambodia = 145,
        Cameroon = 165,
        Canada = 14,
        CapeVerde = 131,
        Chile = 17,
        ChineseTaipei = 52,
        Colombia = 18,
        Comoros = 180,
        CostaRica = 77,
        Croatia = 42,
        Cuba = 93,
        Curacao = 178,
        Cyprus = 82,
        Czechia = 46,
        CoteDIvoire = 132,
        Denmark = 10,
        DominicanRepublic = 83,
        DRCongo = 181,
        Ecuador = 68,
        Egypt = 32,
        ElSalvador = 96,
        England = 2,
        EquatorialGuinea = 201,
        Estonia = 47,
        Ethiopia = 182,
        FaroeIslands = 71,
        Finland = 11,
        France = 5,
        Georgia = 100,
        Germany = 3,
        Ghana = 144,
        Greece = 45,
        Grenada = 194,
        Guam = 179,
        Guatemala = 102,
        Guinea = 198,
        Guyana = 197,
        Haiti = 188,
        Honduras = 95,
        HongKongChina = 53,
        Hungary = 44,
        Iceland = 37,
        India = 27,
        Indonesia = 49,
        Iran = 80,
        Iraq = 135,
        Ireland = 16,
        Israel = 51,
        Italy = 4,
        Jamaica = 89,
        Japan = 25,
        Jordan = 103,
        Kazakhstan = 122,
        Kenya = 90,
        Kuwait = 134,
        Kyrgyzstan = 98,
        Latvia = 48,
        Lebanon = 128,
        Liechtenstein = 125,
        Lithuania = 61,
        Luxembourg = 79,
        Madagascar = 183,
        Malaysia = 39,
        Maldives = 154,
        Malta = 97,
        Mexico = 6,
        Moldova = 99,
        Mongolia = 127,
        Montenegro = 136,
        Morocco = 72,
        Mozambique = 142,
        Myanmar = 189,
        Nepal = 192,
        Netherlands = 12,
        Nicaragua = 121,
        Nigeria = 70,
        NorthMacedonia = 92,
        NorthernIreland = 88,
        Norway = 9,
        Oceania = 13,
        Oman = 140,
        Pakistan = 64,
        Palestine = 166,
        Panama = 91,
        Paraguay = 67,
        PeoplesRepublicOfChina = 28,
        Peru = 21,
        Philippines = 50,
        Poland = 26,
        Portugal = 23,
        PuertoRico = 190,
        Qatar = 149,
        Romania = 36,
        Russia = 34,
        Rwanda = 200,
        SaintKittsAndNevis = 202,
        SaintVincentAndTheGrenadines = 184,
        SanMarino = 191,
        SaoTomeEPrincipe = 177,
        SaudiArabia = 75,
        Scotland = 15,
        Senegal = 86,
        Serbia = 43,
        Singapore = 41,
        Slovakia = 66,
        Slovenia = 57,
        SouthAfrica = 24,
        SouthKorea = 29,
        Spain = 35,
        SriLanka = 175,
        Suriname = 123,
        Sweden = 1,
        Switzerland = 40,
        Syria = 148,
        Tahiti = 196,
        Tanzania = 150,
        Thailand = 30,
        TrinidadAndTobago = 105,
        Tunisia = 76,
        Turkey = 31,
        Uganda = 153,
        Ukraine = 62,
        UnitedArabEmirates = 78,
        Uruguay = 19,
        USA = 8,
        Uzbekistan = 163,
        Venezuela = 20,
        Vietnam = 65,
        Wales = 56,
        Yemen = 139,
        Zambia = 187
    }

    [JsonConverter(typeof(StringEnumConverter))]
    public enum ContinentType
    {
        Any = -1,
        Africa = 1,
        Asia = 3,
        Europe = 4,
        NorthAmerica = 5,
        Oceania = 6,
        SouthAmerica = 7
    }
}
