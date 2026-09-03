namespace Werwolf_Bot.dto;

public class Player(
    long id, 
    string? username, 
    string? firstName, 
    string? lastName, 
    bool isHost, 
    string? role = "dorfbewohner",
    string? sessionId = null) : TelegramUser(id, username, firstName, lastName)
{
    public bool IsHost { get; set; } = isHost;
    public string? Role { get; set; } = role;
    public string? SessionId { get; set; }  = sessionId;
}