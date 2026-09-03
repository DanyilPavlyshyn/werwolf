using Telegram.Bot.Types;

namespace Werwolf_Bot.dto;

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
}