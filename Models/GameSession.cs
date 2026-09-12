using Werwolf_Bot.Models;

namespace Werwolf_Bot.Models;

public class GameSession(long hostId)
{
    private readonly List<string> _selectedRoles = new () ;
    private readonly List<Player> _players = new();
    
    public IReadOnlyList<Player> Players => _players.AsReadOnly();
    public IReadOnlyList<string> SelectedRoles => _selectedRoles.AsReadOnly();
    
    
    public readonly long HostId = hostId;
    public event EventHandler<List<Player>>? OnPlayersChanged;
    private EventHandler<List<Player>>? PlayersChangedHandler { get; set; }
    public readonly string Id = Guid.NewGuid().ToString().Substring(0, 4);

    public void AddPlayer(Player player)
    {
        lock (_players)
        {
            if (_players.Contains(player)) throw new Exception("Player is already in this session.");
            if (_players.Count >= _selectedRoles.Count) throw new Exception("This session is already full.");
            
            player.User.SessionId = Id;
            _players.Add(player);
            OnPlayersChanged?.Invoke(this, _players.ToList());
        }
    }
    
    public void RemovePlayer(TelegramUser user)
    {
        lock (_players)
        {
            var player = _players.FirstOrDefault(x => x.User == user);

            if (player == null) return;
            
            _players.Remove(player);
            player.User.SessionId = null;
            OnPlayersChanged?.Invoke(this, _players.ToList());
        }
    }
    
    public void RemovePlayer(Player player)
    {
        lock (_players)
        {
            if (!_players.Contains(player)) throw new Exception("Player is not in this session.");
            _players.Remove(player);
            OnPlayersChanged?.Invoke(this, _players.ToList());
        }
    }
    
    public void SaveRoleSelection(List<string> roles)
    {
        roles.ForEach(r => _selectedRoles.Add(r));
    }
    
    public void AssignRolesToPlayers()
    {
        if (Players.Count != _selectedRoles.Count)
        {
            throw new Exception("Количество игроков не соответствует количеству ролей.");
        }

        var randomizedRoles = _selectedRoles.ToArray();
        Random.Shared.Shuffle(randomizedRoles);

        for (var i = 0; i < randomizedRoles.Length; i++)
        {
            Players[i].Role = randomizedRoles[i];
        }
    }

    public void AddPlayersObserver(EventHandler<List<Player>> changedHandler)
    {
        RemovePlayersObserver();
        PlayersChangedHandler = changedHandler;
        OnPlayersChanged += changedHandler;
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
}
