using Werwolf_Bot.Models;
using Werwolf_Bot.Services.StepInputHandlers;

namespace Werwolf_Bot.Services;

public enum StepResult
{
    NoChange,
    HostModeSelected,
    PlayerModeSelected,
    RolesChosenByHost,
    ChangeLanguageSelected,
    SessionJoined,
    SessionCanceled,
    LeavedSession,
    GameStarted,
    LanguagedChanged
}

public class StepHandler
{
    private readonly Dictionary<UserStep, Func<TelegramUser, BotUpdate, Task<StepResult>>> _handlers;

    public StepHandler(
        SessionService sessions,
        LocalizationService localization,
        ChatService chat,
        UserService users)
    {
        var choosingLanguage = new ChoosingLanguageHandler();
        var choosingPlayMode = new ChoosingPlayModeHandler();
        var choosingRoles = new ChoosingRolesHandler();
        var enteringSessionId = new EnteringSessionIdHandler();
        var waitingStart = new WaitingStartHandler();
        var readyToStart = new ReadyToStartHandler();
        var waitingPlayersToJoin = new WaitingPlayersToJoinHandler();

        _handlers = new()
        {
            [UserStep.ChoosingLanguage] = AsAsync((user, update) =>
                choosingLanguage.GetResult(user, update, localization)),
            [UserStep.ChoosingPlayMode] = AsAsync((user, update) =>
                choosingPlayMode.GetResult(update)),
            [UserStep.ChoosingRoles] = AsAsync((user, update) =>
                choosingRoles.GetResult(user, update, sessions, chat)),
            [UserStep.EnteringSessionId] = AsAsync((user, update) =>
                enteringSessionId.GetResult(user, update, sessions)),
            [UserStep.WaitingStart] = AsAsync((user, update) =>
                waitingStart.GetResult(user, update, sessions)),
            [UserStep.ReadyToStart] = (user, update) =>
                readyToStart.GetResult(user, update, sessions, chat, users),
            [UserStep.WaitingPlayersToJoin] = AsAsync((user, update) =>
                waitingPlayersToJoin.GetResult(user, update, sessions, users))
        };
    }

    /// <summary>
    /// Executes and awaits the handler registered for the user's current step.
    /// Returns the handler's result, or NoChange if no handler is registered.
    /// Exceptions propagate to the caller.
    /// </summary>
    public async Task<StepResult> HandleAsync(TelegramUser user, BotUpdate update)
    {
        if (!_handlers.TryGetValue(user.Step, out var handler)) return StepResult.NoChange;

        return await handler(user, update);
    }

    private static Func<TelegramUser, BotUpdate, Task<StepResult>> AsAsync(
        Func<TelegramUser, BotUpdate, StepResult> handler)
    {
        return (user, update) => Task.FromResult(handler(user, update));
    }
}
