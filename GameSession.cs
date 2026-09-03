using Werwolf_Bot.dto;

namespace Werwolf_Bot;

public class GameSession(long hostId)
{
    private readonly List<string> _selectedRoles = new ();
    public readonly List<Player> Players = new ();
    public readonly long HostId = hostId;
    public event EventHandler<List<Player>>? OnPlayersChanged;
    public EventHandler<List<Player>>? PlayersChangedHandler { get; set; }
    public readonly string Id = Guid.NewGuid().ToString().Substring(0, 4);

    public class RolesChoice
    {
        public string action { get; set; }
        public List<string> roles { get; set; }
    }

    public void AddPlayerToSession(Player player)
    {
        if (Players.Contains(player)) throw new Exception("Player is already in this session.");
        
        if (Players.Count >= _selectedRoles.Count) throw new Exception("This session is already full.");

        lock (Players)
        {
            player.SessionId = Id;
            Players.Add(player);
            OnPlayersChanged?.Invoke(this, Players.ToList());
        }
    }
    
    public void SaveRoleSelection(List<string> roles)
    {
        roles.ForEach(r => _selectedRoles.Add(r));
    }

    public List<Player> AddPlayersObserver(EventHandler<List<Player>> changedHandler)
    {
        RemovePlayersObserver();
        PlayersChangedHandler = changedHandler;
        OnPlayersChanged += changedHandler;
        return Players;
    }

    public void RemovePlayersObserver()
    {
        if (PlayersChangedHandler == null)
        {
            return;
        }

        OnPlayersChanged -= PlayersChangedHandler;
        PlayersChangedHandler = null;
    }

    public List<string> GetSelectedRoles()
    {
        return _selectedRoles;
    }
    
    public void AssignRolesToPlayers()
    {
        if (Players.Count != _selectedRoles.Count)
        {
            throw new Exception("Количество игроков не соответствует количеству ролей.");
        }

        Random.Shared.Shuffle(_selectedRoles.ToArray());

        for (int i = 0; i < _selectedRoles.Count; i++)
        {
            Players[i].Role = _selectedRoles[i];
        }
    }
}
