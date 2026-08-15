namespace Audix.Utils
{
    public static class TimeFormatter
    {
        public static string Format(int milliseconds)
        {
            var seconds = milliseconds / 1000;
            var minutes = seconds / 60;
            seconds %= 60;
            return $"{minutes:00}:{seconds:00}";
        }
    }
}
