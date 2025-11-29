public static class StringHelpers
{
    public static (string Key, string Value) GetKeyValuePair(this string source)
    {
        if (source.StartsWith('#'))
            return ("", "");
        int colonIndex = source.IndexOf(':');
        if (colonIndex == -1)
        {
            return (string.Empty, string.Empty);
        }

        string key = source[..colonIndex].Trim();
        string value = source[(colonIndex + 1)..].TrimStart();

        return (key, value);
    }
}