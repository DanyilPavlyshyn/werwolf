using System.Text.Json;
using Werwolf_Bot.Models;

namespace Werwolf_Bot.Services.StepInputHandlers;

public class ChoosingRolesHandler
{
    public StepResult GetResult(
        TelegramUser user, 
        BotUpdate update,
        SessionService sessions,
        ChatService chat)
    {
        var gameSession = sessions.CreateSession(user);
        
        if (update.WebData is { } data)
        {
            var result = JsonSerializer
                .Deserialize<RolesChoice>(data);

            if (result?.action == "confirmRoles")
            {
                gameSession.SaveRoleSelection(result.roles);
                gameSession.AddPlayersObserver(async (_, updatedPlayers) =>
                {
                    await chat.SendPlayerListToHostAsync(gameSession);
                });
                return StepResult.RolesChosenByHost;
            }
        }
        return StepResult.NoChange;
    }
}