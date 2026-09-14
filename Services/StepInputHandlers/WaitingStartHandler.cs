using Werwolf_Bot.Models;

namespace Werwolf_Bot.Services.StepInputHandlers;

public class WaitingStartHandler
{
    public StepResult GetResult(
        TelegramUser user,
        BotUpdate update,
        SessionService sessions)
    {
        if (update.Text != "Покинуть игру ❌") return StepResult.NoChange;

        var session = sessions.GetSession(user.SessionId);
        session?.RemovePlayer(user);
        user.SessionId = null;
        return StepResult.LeavedSession;
    }
}
