using System.Collections.Frozen;

namespace FinancialTracker.Domain.Money;

/// <summary>
/// The ISO 4217 currencies a User can keep their finances in, with their minor units.
/// Fund codes, precious metals and testing codes are left out: nobody keeps their finances in them.
/// </summary>
internal static class Iso4217
{
    private static readonly string[] ZeroDecimals =
        ["BIF", "CLP", "DJF", "GNF", "ISK", "JPY", "KMF", "KRW", "PYG", "RWF", "UGX", "VND", "VUV", "XAF", "XOF", "XPF"];

    private static readonly string[] ThreeDecimals = ["BHD", "IQD", "JOD", "KWD", "LYD", "OMR", "TND"];

    private static readonly string[] TwoDecimals =
    [
        "AED", "AFN", "ALL", "AMD", "AOA", "ARS", "AUD", "AWG", "AZN", "BAM", "BBD", "BDT", "BMD", "BND", "BOB",
        "BRL", "BSD", "BTN", "BWP", "BYN", "BZD", "CAD", "CDF", "CHF", "CNY", "COP", "CRC", "CUP", "CVE", "CZK",
        "DKK", "DOP", "DZD", "EGP", "ERN", "ETB", "EUR", "FJD", "FKP", "GBP", "GEL", "GHS", "GIP", "GMD", "GTQ",
        "GYD", "HKD", "HNL", "HTG", "HUF", "IDR", "ILS", "INR", "IRR", "JMD", "KES", "KGS", "KHR", "KPW", "KYD",
        "KZT", "LAK", "LBP", "LKR", "LRD", "LSL", "MAD", "MDL", "MGA", "MKD", "MMK", "MNT", "MOP", "MRU", "MUR",
        "MVR", "MWK", "MXN", "MYR", "MZN", "NAD", "NGN", "NIO", "NOK", "NPR", "NZD", "PAB", "PEN", "PGK", "PHP",
        "PKR", "PLN", "QAR", "RON", "RSD", "RUB", "SAR", "SBD", "SCR", "SDG", "SEK", "SGD", "SHP", "SLE", "SOS",
        "SRD", "SSP", "STN", "SVC", "SYP", "SZL", "THB", "TJS", "TMT", "TOP", "TRY", "TTD", "TWD", "TZS", "UAH",
        "USD", "UYU", "UZS", "VED", "VES", "WST", "XCD", "XCG", "YER", "ZAR", "ZMW", "ZWG",
    ];

    public static readonly FrozenDictionary<string, int> MinorUnitsByCode =
        ZeroDecimals.Select(code => KeyValuePair.Create(code, 0))
            .Concat(TwoDecimals.Select(code => KeyValuePair.Create(code, 2)))
            .Concat(ThreeDecimals.Select(code => KeyValuePair.Create(code, 3)))
            .ToFrozenDictionary(StringComparer.Ordinal);
}
