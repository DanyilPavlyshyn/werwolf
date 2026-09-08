using Telegram.Bot;
using Telegram.Bot.Types;
using Werwolf_Bot.dto;

namespace Werwolf_Bot.services;

public class UserService
{
    private List<TelegramUser> _users = new();
    private readonly Dictionary<string, UserLanguage> _lanCodes = new()
    {
        ["EN"] = UserLanguage.English,
        ["DE"] = UserLanguage.German,
        ["UA"] = UserLanguage.Ukrainian,
        ["RU"] = UserLanguage.Russian
    };
    
    public TelegramUser GetUser(Chat chat)
    {
        var user = _users.FirstOrDefault(u => u.Id == chat.Id, null);
        return user ?? CreateUser(chat);
    }

    private TelegramUser CreateUser(Chat chat)
    {
        var user = new TelegramUser(chat.Id, chat.Username, chat.FirstName, chat.LastName);
        _users.Add(user);
        return user;
    }
    
    public void ChangeUserLanguage(TelegramUser user, string language)
    {
        user.Language = _lanCodes[language];
    }

    public void SetStepForUsers(List<Player> players, UserStep step)
    {
        foreach (var player in players)
        {
            player.Step = step;
        }
    }
}