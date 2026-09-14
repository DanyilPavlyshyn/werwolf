using Werwolf_Bot.Models;

namespace Werwolf_Bot.Services;

public class UpdateProcessor(
    StepHandler handler,
    SessionService sessions,
    UserService users,
    ChatService chat)
{
    // A session is shared by multiple users. Serialize updates through state changes
    // and delivery so joins/leaves cannot change a game while cards are being sent.
    private readonly SemaphoreSlim gate = new(1, 1);

    public async Task HandleAsync(TelegramUser user, BotUpdate update)
    {
        await gate.WaitAsync();
        try
        {
            var previousSession = sessions.GetSession(user.SessionId);
            var result = await handler.HandleAsync(user, update);
            UserStepDispatcher.SetActualStep(user, update, result);

            var changedSession = result switch
            {
                StepResult.SessionJoined => sessions.GetSession(user.SessionId),
                StepResult.LeavedSession => previousSession,
                _ => null
            };
            if (changedSession != null)
            {
                var host = users.FindUser(changedSession.HostId);
                if (host != null) UserStepDispatcher.UpdateHostStep(host, changedSession);
            }

            // All state changes are committed before sending notifications.
            var notifications = new List<Task> { chat.GetStepResponse(user) };
            if (changedSession != null)
                notifications.Add(chat.SendPlayerListToHostAsync(changedSession));
            if (result == StepResult.SessionCanceled && previousSession != null)
                notifications.Add(chat.SendSessionCancelledByHostToPlayers(previousSession.Players.ToList()));
            await Task.WhenAll(notifications);
        }
        finally
        {
            gate.Release();
        }
    }
}
