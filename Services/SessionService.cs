using Werwolf_Bot.dto;

namespace Werwolf_Bot.services;

public class SessionService
{
    private static readonly List<GameSession> ActiveSessions = new();
    
    public GameSession CreateSession(TelegramUser user)
    {
        GameSession session = new GameSession(user.Id);
        ActiveSessions.Add(session);
        user.SessionId = session.Id;
        
        return session;
    }

    public GameSession? JoinSession(string sessionId, Player player)
    {
        var session = ActiveSessions.FirstOrDefault(x => string
            .Equals(x.Id, sessionId, StringComparison.CurrentCultureIgnoreCase), null);
        
        if (session == null)
        {
            return null;
        }
        
        session?.AddPlayerToSession(player);
        player.User.SessionId = sessionId;
        return session;
    }

    public GameSession? GetSession(string? sessionId)
    {
        if (string.IsNullOrEmpty(sessionId)) return null;
        
        return ActiveSessions.FirstOrDefault(x => x.Id == sessionId, null);
    }

    public GameSession? GetGameSessionByHostId(long hostId)
    {
        return ActiveSessions.Find(s => s.HostId == hostId);
    }
    
    public void DeleteSession(GameSession session)
    {
        session.RemovePlayersObserver();
        ActiveSessions.RemoveAll(x => x.Id == session.Id);
        session = null;
    }
}
