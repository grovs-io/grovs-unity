namespace GrovsTestApp
{
    public static class ShareCommands
    {
        public static string Android(string link, string package)
        {
            return $"adb shell am start -a android.intent.action.VIEW -d \"{link}\" {package}";
        }

        public static string Ios(string link)
        {
            return $"xcrun simctl openurl booted \"{link}\"";
        }
    }
}
