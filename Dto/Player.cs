namespace Werwolf_Bot.dto;

public class Player
{
    public TelegramUser User { get; set; }
    public bool IsHost { get; set; }
    public string? Role { get; set; }

    public Player(TelegramUser user, bool isHost, string? role = "dorfbewohner")
    {
        User = user;
        IsHost = isHost;
        Role = role;
    }
}