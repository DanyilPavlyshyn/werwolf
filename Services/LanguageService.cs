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
            "Localization", 
            langCode, 
            "i18n.json"
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

public class LocalizationService
{
    private readonly Dictionary<string, Dictionary<string, string>> _messages = new(StringComparer.OrdinalIgnoreCase);
    private readonly Dictionary<string, Dictionary<string, RoleInfo>> _roles = new(StringComparer.OrdinalIgnoreCase);

    public void LoadLanguage(string langCode)
    {
        string filePath = Path.Combine(
            AppDomain.CurrentDomain.BaseDirectory, 
            "Assets", 
            "Localization", 
            langCode, 
            "i18n.json"
        );

        if (!File.Exists(filePath)) return;

        try
        {
            using FileStream stream = File.OpenRead(filePath);
            using JsonDocument doc = JsonDocument.Parse(stream);

            var messagesForLang = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
            var rolesForLang = new Dictionary<string, RoleInfo>(StringComparer.OrdinalIgnoreCase);

            var options = new JsonSerializerOptions { PropertyNameCaseInsensitive = true };
            
            foreach (JsonProperty prop in doc.RootElement.EnumerateObject())
            {
                // roleInfo object 
                if (prop.Name.Equals("roles", StringComparison.OrdinalIgnoreCase) && prop.Value.ValueKind == JsonValueKind.Object)
                {
                    foreach (JsonProperty roleProp in prop.Value.EnumerateObject())
                    {
                        var roleObj = roleProp.Value.Deserialize<RoleInfo>(options);
                        if (roleObj != null)
                        {
                            rolesForLang[roleProp.Name] = roleObj;
                        }
                    }
                }
                // message string
                else if (prop.Value.ValueKind == JsonValueKind.String)
                {
                    messagesForLang[prop.Name] = prop.Value.GetString() ?? string.Empty;
                }
            }

            _messages[langCode] = messagesForLang;
            _roles[langCode] = rolesForLang;
        }
        catch (Exception ex)
        {
            Console.WriteLine($"[FEHLER] Fehler beim Laden von '{langCode}': {ex.Message}");
        }
    }

    /// <summary> Get message </summary>
    public string GetMessage(string langCode, string key, string fallback = "")
    {
        if (_messages.TryGetValue(langCode, out var langDict) && 
            langDict.TryGetValue(key, out var text))
        {
            return text;
        }
        return fallback;
    }

    /// <summary> Get role object </summary>
    public RoleInfo? GetRole(string langCode, string roleKey)
    {
        if (_roles.TryGetValue(langCode, out var langRoles) && 
            langRoles.TryGetValue(roleKey, out var role))
        {
            return role;
        }
        return null;
    }
}