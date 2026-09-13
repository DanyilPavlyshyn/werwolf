using System.Text.Json;
using Werwolf_Bot.Models;

namespace Werwolf_Bot.Services;

public class StepHandler
{
    private readonly SessionService sessions;
    private readonly LocalizationService localization;
    private readonly ChatService chat;
    private readonly UserService users;
    private readonly Dictionary<UserStep, Func<TelegramUser, BotUpdate, Task>> _handlers;

    public StepHandler(
        SessionService sessions,
        LocalizationService localization,
        ChatService chat,
        UserService users)
    {
        this.sessions = sessions;
        this.localization = localization;
        this.chat = chat;
        this.users = users;

        _handlers = new()
        {
            [UserStep.ChoosingLanguage] = AsAsync(HandleChoosingLanguage),
            [UserStep.ChoosingRoles] = AsAsync(HandleChoosingRoles),
            [UserStep.EnteringSessionId] = AsAsync(HandleEnteringSessionId),
            [UserStep.StartedAsHost] = HandleStartedAsHost,
            [UserStep.CanceledAsHost] = AsAsync(HandleCanceledAsHost),
            [UserStep.CanceledAsRole] = AsAsync(HandleCanceledAsRole)
        };
    }

    /// <summary>
    /// Executes and awaits the handler registered for the user's current step.
    /// Returns false if no handler is registered; true means the handler completed,
    /// not necessarily that the input was valid. Exceptions propagate to the caller.
    /// </summary>
    public async Task<bool> HandleAsync(TelegramUser user, BotUpdate update)
    {
        if (!_handlers.TryGetValue(user.Step, out var handler)) return false;

        await handler(user, update);
        return true;
    }

    private static Func<TelegramUser, BotUpdate, Task> AsAsync(
        Action<TelegramUser, BotUpdate> handler)
    {
        return (user, update) =>
        {
            handler(user, update);
            return Task.CompletedTask;
        };
    }

    private void HandleChoosingLanguage(TelegramUser user, BotUpdate message)
    {
        if (message.Text == null) return;
        var language = localization.GetUserLanguage(message.Text);
        if (language == null) return;

        user.SetLanguage(language.Value);
    }

    private void HandleChoosingRoles(TelegramUser user, BotUpdate update)
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

    private void HandleCanceledAsHost(TelegramUser user, BotUpdate message)
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

    private async Task HandleStartedAsHost(TelegramUser user, BotUpdate message)
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

    private void HandleCanceledAsRole(TelegramUser user, BotUpdate message)
    {
        var gameSession = sessions.GetSession(user.SessionId);
        gameSession?.RemovePlayer(user);
    }

    private void HandleEnteringSessionId(TelegramUser user, BotUpdate message)
    {
        var session = sessions.GetSession(message.Text);
        if (session is null) throw new BusinessException("Ошибка Id! Проверь правильность Id и введи еще раз.");
            
        var player = new Player(user, false);
        session.AddPlayer(player);
    }
}
