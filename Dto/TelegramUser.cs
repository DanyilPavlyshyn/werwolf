using Telegram.Bot.Types;

namespace Werwolf_Bot.dto;

public enum UserLanguage
{
    English,
    German,
    Ukrainian,
    Russian
}

public enum UserStep
{
    None,
    ChooseLanguage,
    ChoosePlayMode,
    EnterSessionId,
    AwaitingRole,
    ChooseRoles,
    WaitingPlayersToJoin,
    GameStarted
}

public class TelegramUser(
    long id,
    string? username,
    string? firstName,
    string? lastName)
{
    public long Id { get; set; } = id;
    public string? Username { get; set; } = username;
    public string? FirstName { get; set; } = firstName;
    public string? LastName { get; set; } = lastName;
    public string? SessionId { get; set; }  = null;
    public UserStep Step { get; set; } = UserStep.None;
    public UserLanguage Language { get; set; } = UserLanguage.English;
    
    public bool SetLanguage(string language)
    {
        bool languageSet = false;
        
        switch (language)
        {
            case "EN 🇬🇧":
                Language = UserLanguage.English;
                languageSet = true;
                break;
            case "DE 🇩🇪":
                Language = UserLanguage.German;
                languageSet = true;
                break;
            case "UA 🇺🇦":
                Language = UserLanguage.Ukrainian;
                languageSet = true;
                break;
            case "EU 🇷🇺":
                Language = UserLanguage.Russian;
                languageSet = true;
                break;
        }
        
        return languageSet;
    }

    public void SetStep(UserStep step)
    {
        Step = step;
    }
    
    public void ClearStep()
    {
        Step = UserStep.None;
    }
}
