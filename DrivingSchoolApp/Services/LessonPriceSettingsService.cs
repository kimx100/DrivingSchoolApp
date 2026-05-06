using System.Globalization;
using DrivingSchoolApp.Models;

namespace DrivingSchoolApp.Services;

public static class LessonPriceSettingsService
{
    public const decimal DefaultDrivingLessonPrice = 1000m;
    public const decimal DefaultTheoryLessonPrice = 250m;
    public const string Currency = "DKK";

    private const string DrivingLessonPriceKey = "DefaultDrivingLessonPrice";
    private const string TheoryLessonPriceKey = "DefaultTheoryLessonPrice";

    public static decimal GetDefaultDrivingLessonPrice()
        => (decimal)Preferences.Get(DrivingLessonPriceKey, (double)DefaultDrivingLessonPrice);

    public static decimal GetDefaultTheoryLessonPrice()
        => (decimal)Preferences.Get(TheoryLessonPriceKey, (double)DefaultTheoryLessonPrice);

    public static void SaveDefaultPrices(decimal drivingLessonPrice, decimal theoryLessonPrice)
    {
        Preferences.Set(DrivingLessonPriceKey, (double)drivingLessonPrice);
        Preferences.Set(TheoryLessonPriceKey, (double)theoryLessonPrice);
    }

    public static Money CreateMoney(decimal amount)
        => new(amount, Currency);

    public static bool TryParsePrice(string? text, out decimal amount)
    {
        text = text?.Trim().Replace(',', '.') ?? string.Empty;

        return decimal.TryParse(
                   text,
                   NumberStyles.Number,
                   CultureInfo.InvariantCulture,
                   out amount) &&
               amount >= 0;
    }

    public static string FormatPrice(decimal amount)
        => amount.ToString("0.##", CultureInfo.InvariantCulture);
}
