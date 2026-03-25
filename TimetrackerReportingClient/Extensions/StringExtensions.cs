namespace TimetrackerReportingClient.Extensions;

public static class StringExtensions
{
    public static string EnsureStringIsNotNullOrEmpty(this string parameter)
    {
        if (string.IsNullOrEmpty(parameter))
        {
            throw new ArgumentException("String cannot be null or empty.", nameof(parameter));
        }
        return parameter;
    }

    public static (int Year, int Month) ParseToMonth(this string input)
    {
        if (
            DateTime.TryParseExact(
                input,
                "MMMM",
                System.Globalization.CultureInfo.InvariantCulture,
                System.Globalization.DateTimeStyles.None,
                out var parsed
            )
        )
        {
            return (DateTime.Today.Year, parsed.Month);
        }

        if (int.TryParse(input, out var month) && month >= 1 && month <= 12)
        {
            return (DateTime.Today.Year, month);
        }

        throw new ArgumentException(
            $"Could not parse '{input}' as a month. Try: April, Apr, or 4."
        );
    }
}
