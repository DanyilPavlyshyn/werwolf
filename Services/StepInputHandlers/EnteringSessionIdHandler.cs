using Werwolf_Bot.Models;

namespace Werwolf_Bot.Services.StepInputHandlers;

public class EnteringSessionIdHandler(LocalizationService localization)
{
    public StepResult GetResult(
        TelegramUser user, 
        BotUpdate message, 
        SessionService sessions)
    {
        if (localization.Matches(user.Language, message.Text, "button.leaveSession"))
        {
            var gameSession = sessions.GetSession(user.SessionId);
            gameSession?.RemovePlayer(user);
            user.SessionId = null;
            return StepResult.LeavedSession;
        }
        
        var session = sessions.GetSession(message.Text);
        
        if (session is null)
        {
            throw new BusinessException("invalidSessionId");
        }
            
        var player = new Player(user, false);
        session.AddPlayer(player);
        return StepResult.SessionJoined;
    }
}