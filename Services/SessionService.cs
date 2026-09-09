using Werwolf_Bot.dto;

namespace Werwolf_Bot.services;

public class SessionService
{
    private static readonly List<GameSession> ActiveSessions = new();
    
    public GameSession CreateSession(long hostId)
    {
        GameSession session = new GameSession(hostId);
        ActiveSessions.Add(session);
        
        return session;
    }

    public GameSession JoinSession(string sessionId, Player player)
    {
        try
        {
            var session = ActiveSessions.Single(x => string
                .Equals(x.Id, sessionId, StringComparison.CurrentCultureIgnoreCase));
            player.User.SessionId = sessionId;
            session.AddPlayerToSession(player);
                
            return session;
        }
        catch (Exception e)
        {
            Console.WriteLine(e);
            throw;
        }
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
