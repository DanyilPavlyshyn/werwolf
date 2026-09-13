using Telegram.Bot.Types;

namespace Werwolf_Bot.Models;

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
    LanguageChosed,
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
    public string? SessionId { get; set; }
    public UserStep Step { get; set; } = UserStep.None;
    public UserLanguage? Language { get; set; }
    
    public void SetLanguage(UserLanguage language)
    {
        Language = language; 
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
