public static class StringExtensions
{
    public static string AT_Command(this string input, string command = "\r")
    {
        return input.Replace(command, "").Replace("\r", "").Replace("\n", "").Replace("OK", "");
    }
}