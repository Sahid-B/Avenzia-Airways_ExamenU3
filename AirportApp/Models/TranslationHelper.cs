using System;
using System.Linq;
using System.Text.Json;

namespace AirportApp
{
    public static class TranslationHelper
    {
        public static string Get(string? json, string lang = "en")
        {
            if (string.IsNullOrEmpty(json)) return string.Empty;
            if (!json.TrimStart().StartsWith("{")) return json;

            try
            {
                using var doc = JsonDocument.Parse(json);
                var root = doc.RootElement;
                if (root.TryGetProperty(lang, out var prop))
                {
                    return prop.GetString() ?? string.Empty;
                }
                if (lang != "en" && root.TryGetProperty("en", out var enProp))
                {
                    return enProp.GetString() ?? string.Empty;
                }
                var first = root.EnumerateObject().FirstOrDefault();
                return first.Value.GetString() ?? json;
            }
            catch
            {
                return json;
            }
        }
    }
}
