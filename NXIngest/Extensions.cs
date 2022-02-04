namespace NXIngest
{
    public static class Extensions
    {
        public static string Capitalized(this string s) => s[..1].ToUpper() + s[1..];
    }
}
