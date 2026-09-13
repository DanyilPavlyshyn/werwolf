using System.Text.Json;
using Werwolf_Bot.Models;

namespace Werwolf_Bot.Services;

public class StepHandler(
    SessionService sessions,
    LocalizationService localization,
    ChatService chat,
    UserService users)
{
    private readonly Dictionary<UserStep, Func<TelegramUser, BotUpdate>> _handlers = new()
    {
        [UserStep.ChoosingLanguage] = HandleChoosingLanguage,
        [UserStep.ChoosingRoles] = HandleChoosingRoles,
        [UserStep.EnteringSessionId] = HandleEnteringSessionId,
        [UserStep.StartedAsHost] = HandleStartedAsHost,
        [UserStep.CanceledAsHost] = HandleCanceledAsHost,
        [UserStep.CanceledAsRole] = HandleCanceledAsRole
    };

    /// <summary>
    /// Processes input for the current step, before the dispatcher changes it.
    /// False means ignored or invalid input; do not advance the step in that case.
    /// Navigation-only and completed steps have no data operation.
    /// This class neither changes steps nor sends messages.
    /// </summary>
    public bool Handle(TelegramUser user, BotUpdate update)
    {
        return _handlers.TryGetValue(user.Step, out var handler)
               && handler(user, update);
    }

    public void HandleChoosingLanguage(TelegramUser user, BotUpdate message)
    {
        if (message.Text == null) return;
        var language = localization.GetUserLanguage(message.Text);
        if (language == null) return;

        user.SetLanguage(language.Value);
    }

    public void HandleChoosingRoles(TelegramUser user, BotUpdate update)
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
            }
        }
    }

    public void HandleCanceledAsHost(TelegramUser user, BotUpdate message)
    {
        var gameSession = sessions.GetSession(user.SessionId);
    
        if (gameSession == null) 
        {
            throw new BusinessException("Произошла ошибка. Создай новую игру или присоеденись.");
        }
        users.SetStepForUsers(gameSession.Players, UserStep.None);
        user.SessionId = null;
        sessions.DeleteSession(gameSession);
    }

    public async Task HandleStartedAsHost(TelegramUser user, BotUpdate message)
    {
        var gameSession = sessions.GetSession(user.SessionId);
        
        if (gameSession == null) 
        {
            throw new BusinessException("Произошла ошибка. Создай новую игру или присоеденись.");
        }

        await chat.SendRoleCardsToPlayersAsync(gameSession);
        await chat.SendPlayersAndRolesToHostAsync(gameSession);
        await chat.SendRulesToHostAsync(gameSession);
        users.SetStepForUsers(gameSession.Players, UserStep.None);
        user.SessionId = null;
        sessions.DeleteSession(gameSession);
    }

    public void HandleCanceledAsRole(TelegramUser user, BotUpdate message)
    {
        var gameSession = sessions.GetSession(user.SessionId);
        gameSession?.RemovePlayer(user);
    }

    public void HandleEnteringSessionId(TelegramUser user, BotUpdate message)
    {
        var session = sessions.GetSession(message.Text);
        if (session is null) throw new BusinessException("Ошибка Id! Проверь правильность Id и введи еще раз.");
            
        var player = new Player(user, false);
        session.AddPlayer(player);
    }
}
