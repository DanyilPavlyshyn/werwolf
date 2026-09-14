using Werwolf_Bot.Models;

namespace Werwolf_Bot.Services.StepInputHandlers;

public class ReadyToStartHandler
{
    public async Task<StepResult> GetResult(
        TelegramUser user, 
        BotUpdate message,
        SessionService sessions,
        ChatService chat,
        UserService users)
    {
        if (message is { Text: "Раздать карты 🃏" })
        {
            var gameSession = sessions.GetSession(user.SessionId);

            if (gameSession == null)
            {
                user.SessionId = null;
                throw new BusinessException("Произошла ошибка. Создай новую игру или присоеденись.");
            }

            await chat.SendRoleCardsToPlayersAsync(gameSession);
            await chat.SendPlayersAndRolesToHostAsync(gameSession);
            await chat.SendRulesToHostAsync(gameSession);
            users.SetStepForUsers(gameSession.Players, UserStep.SessionStartedByHost);
            user.SessionId = null;
            sessions.DeleteSession(gameSession);
            return StepResult.GameStarted;
        }

        if (message is { Text: "Отменить игру ❌" })
        {
            var gameSession = sessions.GetSession(user.SessionId);

            if (gameSession == null)
            {
                user.SessionId = null;
                throw new BusinessException("Игра отменена. Пиши, как захочешь поиграть. :)");
            }
            
            users.SetStepForUsers(gameSession.Players, UserStep.SessionCancelledByHost);
            user.SessionId = null;
            sessions.DeleteSession(gameSession);
            return StepResult.SessionCanceled;
        }
        
        return StepResult.NoChange;
    }
}