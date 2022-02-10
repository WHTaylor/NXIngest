using System;

namespace NXIngest.Output
{
    /// <summary>
    /// Format datetimes using mapping file format strings
    ///
    /// The nxingest mapping files use formatting strings from [strftime]
    /// (https://www.cplusplus.com/reference/ctime/strftime/).
    /// </summary>
    public static class DateTimeFormatter
    {
        public static string Format(DateTime time, string originalFormatString) =>
            time.ToString(TranslateFormatString(originalFormatString));

        private static string TranslateFormatString(string original)
        {
            return original
                .Replace("%Y", "yyyy")
                .Replace("%m", "MM")
                .Replace("%d", "dd")
                .Replace("%H", "HH")
                .Replace("%M", "mm")
                .Replace("%S", "ss");
        }
    }
}
