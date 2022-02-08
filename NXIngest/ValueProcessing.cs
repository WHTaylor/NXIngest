using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;

namespace NXIngest
{
    public static class ValueProcessing
    {
        private static readonly HashSet<string> CommonWords = new(
            new List<string>
            {
                "the", "be", "to", "of", "and", "a", "in", "that", "have", "i",
                "for", "on", "with", "he", "as", "do", "at", "this", "but", "by",
                "from", "we", "or", "an", "would", "so", "about", "get", "which",
                "go", "when", "can", "like", "just", "into", "them", "other",
                "than", "then", "its", "over", "also", "use", "how", "our",
                "work", "well", "because", "us", "is"
            });

        private const string AllPunctuationPattern = "^[^a-zA-Z0-9]+$";

        public static IEnumerable<string> ToKeywords(string value)
        {
            return value.Replace(",", " ")
                .Split()
                .Where(s => !string.IsNullOrWhiteSpace(s))
                .Where(IsAcceptableKeyword);
        }

        private static bool IsAcceptableKeyword(string kw) =>
            !CommonWords.Contains(kw)
            && !Regex.IsMatch(kw, AllPunctuationPattern);
    }
}
