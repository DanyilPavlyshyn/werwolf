using Werwolf_Bot.Models;

namespace Werwolf_Bot.Services.StepInputHandlers;

public class WaitingPlayersToJoinHandler
{
    public StepResult GetResult(
        TelegramUser user,
        BotUpdate update,
        SessionService sessions,
        UserService users)
    {
        if (update.Text != "Отменить игру ❌") return StepResult.NoChange;

        var session = sessions.GetSession(user.SessionId);
        if (session == null)
        {
            user.SessionId = null;
            return StepResult.SessionCanceled;
        }

        users.SetStepForUsers(session.Players, UserStep.SessionCancelledByHost);
        user.SessionId = null;
        sessions.DeleteSession(session);
        return StepResult.SessionCanceled;
    }
}
