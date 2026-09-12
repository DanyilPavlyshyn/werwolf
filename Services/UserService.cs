using System.Collections.Concurrent;
using Telegram.Bot.Types;
using Werwolf_Bot.Models;

namespace Werwolf_Bot.Services;

public class UserService
{
    private readonly ConcurrentDictionary<long, TelegramUser> _users = new();
    
    public TelegramUser GetUser(Chat chat)
    {
        var user = _users.GetValueOrDefault(chat.Id, null);
        return user ?? CreateUser(chat);
    }

    private TelegramUser CreateUser(Chat chat)
    {
        var user = new TelegramUser(chat.Id, chat.Username, chat.FirstName, chat.LastName);
        _users[chat.Id] = user;
        return user;
    }
    
    public void SetStepForUsers(List<TelegramUser> users, UserStep step)
    {
        foreach (var user in users)
        {
            user.SetStep(step);
        }
    }
    
    public void SetStepForUsers(IReadOnlyList<Player> players, UserStep step)
    {
        foreach (var player in players)
        {
            if (_users.TryGetValue(player.User.Id, out var user))
            {
                user.SetStep(step);
            }
        }
    }
}