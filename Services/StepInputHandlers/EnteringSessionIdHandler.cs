using Werwolf_Bot.Models;

namespace Werwolf_Bot.Services.StepInputHandlers;

public class EnteringSessionIdHandler
{
    public StepResult GetResult(
        TelegramUser user, 
        BotUpdate message, 
        SessionService sessions)
    {
        if (message is { Text: "Покинуть игру ❌" })
        {
            var gameSession = sessions.GetSession(user.SessionId);
            gameSession?.RemovePlayer(user);
            return StepResult.LeavedSession;
        }
        
        var session = sessions.GetSession(message.Text);
        
        if (session is null)
        {
            throw new BusinessException("Отключено. Пиши, если захочешь поиграть. :)");
        }
            
        var player = new Player(user, false);
        session.AddPlayer(player);
        return StepResult.SessionJoined;
    }
}