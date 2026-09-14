using Werwolf_Bot.Models;

namespace Werwolf_Bot.Services.StepInputHandlers;

public class ReadyToStartHandler(LocalizationService localization)
{
    public async Task<StepResult> GetResult(
        TelegramUser user, 
        BotUpdate message,
        SessionService sessions,
        ChatService chat,
        UserService users)
    {
        if (localization.Matches(user.Language, message.Text, "button.dealCards"))
        {
            var gameSession = sessions.GetSession(user.SessionId);

            if (gameSession == null)
            {
                user.SessionId = null;
                throw new BusinessException("sessionRecoveryError");
            }

            await chat.SendRoleCardsToPlayersAsync(gameSession);
            await chat.SendPlayersAndRolesToHostAsync(gameSession);
            await chat.SendRulesToHostAsync(gameSession);
            users.SetStepForUsers(gameSession.Players, UserStep.SessionStartedByHost);
            user.SessionId = null;
            sessions.DeleteSession(gameSession);
            return StepResult.GameStarted;
        }

        if (localization.Matches(user.Language, message.Text, "button.cancelSession"))
        {
            return new WaitingPlayersToJoinHandler(localization).GetResult(user, message, sessions, users);
        }
        
        return StepResult.NoChange;
    }
}