namespace Werwolf_Bot.Models;

public class RolesChoice
{
    public string action { get; set; } = string.Empty;
    public List<string> roles { get; set; } = new();
}