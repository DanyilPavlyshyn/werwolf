using System.Text.Json;
using System.Text.Json.Serialization;

namespace Werwolf_Bot.services;

public class GameData
{
    [JsonPropertyName("roles")]
    public Dictionary<string, RoleInfo> Roles { get; set; } = new();
}

public class RoleInfo
{
    [JsonPropertyName("title")]
    public string Title { get; set; } = string.Empty;

    [JsonPropertyName("description")]
    public string Description { get; set; } = string.Empty;

    [JsonPropertyName("nightPrio")]
    public int NightPrio { get; set; }

    [JsonPropertyName("onlyFirstNight")]
    public bool OnlyFirstNight { get; set; }
}

public class LanguageService(string langCode)
{
    public RoleInfo? GetRole(string roleKey)
    {
        string filePath = Path.Combine(
            AppDomain.CurrentDomain.BaseDirectory, 
            "Assets", 
            "local", 
            langCode, 
            "rules.json"
        );

        if (!File.Exists(filePath))
        {
            Console.WriteLine($"[Error] File has not been found: {filePath}");
            return null;
        }

        try
        {
            string jsonString = File.ReadAllText(filePath);

            var options = new JsonSerializerOptions
            {
                PropertyNameCaseInsensitive = true
            };

            GameData? data = JsonSerializer.Deserialize<GameData>(jsonString, options);

            if (data != null && data.Roles.TryGetValue(roleKey, out var roleInfo))
            {
                return roleInfo;
            }

            Console.WriteLine($"[WARNUNG] Role '{roleKey}' in '{langCode}' not found.");
            return null;
        }
        catch (Exception ex)
        {
            Console.WriteLine($"[Error] while reading JSON: {ex.Message}");
            return null;
        }
    }
}