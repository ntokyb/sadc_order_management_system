namespace SadcOrders.Domain.ValueObjects;

public sealed class SadcCountryCurrency
{
    private static readonly Dictionary<string, string[]> ValidPairings = new(StringComparer.OrdinalIgnoreCase)
    {
        ["ZA"] = ["ZAR"],
        ["BW"] = ["BWP"],
        ["ZW"] = ["ZWL", "USD"],
        ["NA"] = ["NAD", "ZAR"],
        ["LS"] = ["LSL", "ZAR"],
        ["SZ"] = ["SZL", "ZAR"],
        ["MZ"] = ["MZN"],
        ["ZM"] = ["ZMW"],
        ["TZ"] = ["TZS"],
        ["KE"] = ["KES"],
        ["UG"] = ["UGX"],
        ["MW"] = ["MWK"]
    };

    public string CountryCode { get; }
    public string CurrencyCode { get; }
    public string? ErrorMessage { get; private set; }

    private SadcCountryCurrency(string countryCode, string currencyCode)
    {
        CountryCode = countryCode;
        CurrencyCode = currencyCode;
    }

    public static SadcCountryCurrency Create(string countryCode, string currencyCode) =>
        new(countryCode.Trim().ToUpperInvariant(), currencyCode.Trim().ToUpperInvariant());

    public static bool IsSupportedCountry(string countryCode) =>
        ValidPairings.ContainsKey(countryCode.Trim().ToUpperInvariant());

    public bool IsValid()
    {
        if (!ValidPairings.TryGetValue(CountryCode, out var currencies))
        {
            ErrorMessage = $"Country code '{CountryCode}' is not a supported SADC country.";
            return false;
        }

        if (!currencies.Contains(CurrencyCode))
        {
            ErrorMessage =
                $"Currency '{CurrencyCode}' is not valid for country '{CountryCode}'. Allowed: {string.Join(", ", currencies)}.";
            return false;
        }

        ErrorMessage = null;
        return true;
    }
}
