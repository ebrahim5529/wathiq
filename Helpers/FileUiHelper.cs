using System.Globalization;
using HR.Models;

namespace HR.Helpers;

/// <summary>
/// أدوات مساعدة لعرض عناصر الأرشيف (أيقونات، أحجام، تواريخ).
/// </summary>
public static class FileUiHelper
{
    private static readonly string[] Months =
    [
        "يناير", "فبراير", "مارس", "أبريل", "مايو", "يونيو",
        "يوليو", "أغسطس", "سبتمبر", "أكتوبر", "نوفمبر", "ديسمبر"
    ];

    /// <summary>أيقونة Bootstrap حسب امتداد الملف.</summary>
    public static string IconClass(string? extension) => (extension ?? string.Empty).TrimStart('.').ToLowerInvariant() switch
    {
        "pdf" => "bi-file-earmark-pdf-fill text-danger",
        "doc" or "docx" or "rtf" => "bi-file-earmark-word-fill text-info",
        "xls" or "xlsx" or "csv" => "bi-file-earmark-spreadsheet-fill text-success",
        "png" or "jpg" or "jpeg" or "gif" or "webp" or "bmp" or "svg" => "bi-file-earmark-image-fill text-primary",
        "zip" or "rar" or "7z" => "bi-file-earmark-zip-fill text-secondary",
        "mp4" or "mov" or "avi" or "mkv" => "bi-file-earmark-play-fill text-warning",
        "mp3" or "wav" => "bi-file-earmark-music-fill text-warning",
        "txt" or "md" => "bi-file-earmark-text-fill text-secondary",
        "cs" or "ts" or "js" or "json" or "html" or "css" or "xml" or "sql" => "bi-file-earmark-code-fill text-primary",
        _ => "bi-file-earmark-fill text-secondary"
    };

    /// <summary>اسم عربي لنوع المستند.</summary>
    public static string TypeName(DocumentType type) => type switch
    {
        DocumentType.Official => "مستند رسمي",
        DocumentType.Contract => "عقد",
        DocumentType.Correspondence => "مراسلات",
        DocumentType.Invoice => "فاتورة",
        _ => "عام"
    };

    /// <summary>تنسيق حجم الملف بشكل مقروء.</summary>
    public static string FormatSize(long bytes)
    {
        if (bytes < 1024) return $"{bytes} بايت";
        double kb = bytes / 1024.0;
        if (kb < 1024) return $"{kb:0.#} KB";
        double mb = kb / 1024.0;
        if (mb < 1024) return $"{mb:0.##} MB";
        return $"{mb / 1024.0:0.##} GB";
    }

    /// <summary>لون نص مقروء (أبيض أو كحلي) فوق خلفية بلون Hex مثل #0072ce.</summary>
    public static string ContrastText(string? hexColor)
    {
        var hex = (hexColor ?? string.Empty).Trim().TrimStart('#');
        if (hex.Length == 3) hex = string.Concat(hex.Select(c => $"{c}{c}"));
        if (hex.Length != 6 || !int.TryParse(hex, NumberStyles.HexNumber, CultureInfo.InvariantCulture, out var rgb))
            return "#fff";

        static double Channel(int v)
        {
            var c = v / 255.0;
            return c <= 0.03928 ? c / 12.92 : Math.Pow((c + 0.055) / 1.055, 2.4);
        }

        var luminance = 0.2126 * Channel((rgb >> 16) & 0xFF) + 0.7152 * Channel((rgb >> 8) & 0xFF) + 0.0722 * Channel(rgb & 0xFF);
        return luminance > 0.179 ? "#071e3d" : "#fff";
    }

    /// <summary>تاريخ نسبي بالعربية (اليوم/أمس/يوم شهر). القيمة الداخلة تكون UTC.</summary>
    public static string RelativeDate(DateTime? utcDate)
    {
        if (utcDate is not { } utc) return "—";
        try
        {
            var local = TimeZoneInfo.ConvertTimeFromUtc(utc, TimeZoneInfo.Local);
            var now = DateTime.Now;
            if (local.Date == now.Date) return local.ToString("hh:mm", CultureInfo.InvariantCulture) + (local.Hour < 12 ? " ص" : " م");
            if (local.Date == now.Date.AddDays(-1)) return "أمس";
            if (local.Year == now.Year) return $"{local.Day} {Months[local.Month - 1]}";
            return $"{local.Day} {Months[local.Month - 1]} {local.Year}";
        }
        catch (ArgumentException)
        {
            return "—";
        }
    }
}
