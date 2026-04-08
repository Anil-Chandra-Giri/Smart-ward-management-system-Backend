namespace Smart_ward_management_system.Extensions
{
    public static class LoggingExtensions
    {
        public static string Truncate(this string value, int maxLength)
        {
            if (string.IsNullOrEmpty(value)) return string.Empty;
            if (value.Length <= maxLength) return value;
            return value.Substring(0, maxLength) + "...";
        }
    }
}
