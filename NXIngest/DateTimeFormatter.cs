using System;

namespace NXIngest
{
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
