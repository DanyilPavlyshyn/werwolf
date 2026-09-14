using Werwolf_Bot.Models;

namespace Werwolf_Bot.Services;

public class SessionService
{
    private readonly List<GameSession> activeSessions = new();

    public GameSession CreateSession(TelegramUser user)
    {
        lock (activeSessions)
        {
            if (GetSession(user.SessionId) != null)
                throw new BusinessException("Ты уже в игре.");
            GameSession session;
            do { session = new GameSession(user.Id); }
            while (activeSessions.Any(s => s.Id == session.Id));
            activeSessions.Add(session);
            user.SessionId = session.Id;
            return session;
        }
    }

    public GameSession? GetSession(string? sessionId)
    {
        lock (activeSessions)
        {
            return activeSessions.FirstOrDefault(s =>
                string.Equals(s.Id, sessionId?.Trim(), StringComparison.OrdinalIgnoreCase));
        }
    }

    public void DeleteSession(GameSession session)
    {
        lock (activeSessions)
        {
            foreach (var player in session.Players)
                if (player.User.SessionId == session.Id) player.User.SessionId = null;
            activeSessions.Remove(session);
        }
    }
}
