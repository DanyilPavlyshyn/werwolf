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
            user.SessionId = null;
            return StepResult.LeavedSession;
        }
        
        var session = sessions.GetSession(message.Text);
        
        if (session is null)
        {
            throw new BusinessException("Ошибка Id! Проверь правильность Id и введи еще раз.");
        }
            
        var player = new Player(user, false);
        session.AddPlayer(player);
        return StepResult.SessionJoined;
    }
}