using Werwolf_Bot.Models;

namespace Werwolf_Bot.Services.StepInputHandlers;

public class WaitingStartHandler(LocalizationService localization)
{
    public StepResult GetResult(
        TelegramUser user,
        BotUpdate update,
        SessionService sessions)
    {
        if (!localization.Matches(user.Language, update.Text, "button.leaveSession")) return StepResult.NoChange;

        var session = sessions.GetSession(user.SessionId);
        session?.RemovePlayer(user);
        user.SessionId = null;
        return StepResult.LeavedSession;
    }
}
