using Werwolf_Bot.Models;

namespace Werwolf_Bot.Services;

public class SessionService
{
    private static readonly List<GameSession> ActiveSessions = new();
    
    public GameSession CreateSession(TelegramUser user)
    {
        var session = new GameSession(user.Id);

        lock (ActiveSessions)
        {
            ActiveSessions.Add(session);
        }
        
        user.SessionId = session.Id;
        return session;
    }

    public GameSession? GetSession(string? sessionId)
    {
        var session = ActiveSessions
            .FirstOrDefault(x => x.Id == sessionId, null);

        return session;
    }
    
    public void DeleteSession(GameSession session)
    {
        session.RemovePlayersObserver();
        ActiveSessions.RemoveAll(x => x.Id == session.Id);
        session = null;
    }
}
