using System.Globalization;

namespace Trainer.Web.Core.Story;

/// <summary>
/// Converts a date and time into a TNG-style stardate (flavour only).
/// Fan convention: 1000 units per year, 41000.0 at the start of 2364, and the
/// decimals are the fraction of the year that has passed. One unit is about 8.8 hours.
/// The real year is moved into the 24th century so that 2026 reads as 2370 (47xxx),
/// which matches the story's stardates.
/// </summary>
public static class Stardate
{
    /// <summary>Years added to the real year: 2026 + 344 = 2370.</summary>
    public const int YearOffset = 344;

    public static double From(DateTime time)
    {
        var startOfYear = new DateTime(time.Year, 1, 1, 0, 0, 0, time.Kind);
        var yearLength = startOfYear.AddYears(1) - startOfYear;
        var fraction = (time - startOfYear) / yearLength;
        return 1000 * (time.Year + YearOffset - 2323 + fraction);
    }

    /// <summary>One decimal, cut off rather than rounded, so 47999.97 never shows as 48000.0.</summary>
    public static string Format(DateTime time) =>
        (Math.Floor(From(time) * 10) / 10).ToString("0.0", CultureInfo.InvariantCulture);
}
