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
    public UserStep Step { get; set; } = UserStep.None;
    public UserLanguage Language { get; set; } = UserLanguage.English;
    
    public void SetLanguage(string language)
    {
        switch (language)
        {
            case "EN 🇬🇧":
                Language = UserLanguage.English;
                break;
            case "DE 🇩🇪":
                Language = UserLanguage.German;
                break;
            case "UA 🇺🇦":
                Language = UserLanguage.Ukrainian;
                break;
            case "EU 🇷🇺":
                Language = UserLanguage.Russian;
                break;
        }
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
