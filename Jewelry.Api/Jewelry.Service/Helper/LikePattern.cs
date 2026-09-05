namespace Jewelry.Service.Helper
{
    public static class LikePattern
    {
        public static string EscapeLikePattern(string text)
        {
            return text.Replace("\\", "\\\\").Replace("%", "\\%").Replace("_", "\\_");
        }
    }
}
