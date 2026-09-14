using Werwolf_Bot.Models;

namespace Werwolf_Bot.Models;

public class GameSession(long hostId)
{
    private readonly List<string> _selectedRoles = new () ;
    private readonly List<Player> _players = new();
    
    public IReadOnlyList<Player> Players => _players.AsReadOnly();
    public IReadOnlyList<string> SelectedRoles => _selectedRoles.AsReadOnly();
    
    
    public readonly long HostId = hostId;
    public bool RolesAssigned { get; private set; }
    public readonly string Id = Guid.NewGuid().ToString().Substring(0, 4);

    public void AddPlayer(Player player)
    {
        lock (_players)
        {
            if (RolesAssigned) throw new BusinessException("Игра уже начинается.");
            if (_players.Any(p => p.User.Id == player.User.Id)) throw new BusinessException("Player is already in this session.");
            if (_players.Count >= _selectedRoles.Count) throw new BusinessException("This session is already full.");
            
            player.User.SessionId = Id;
            _players.Add(player);
        }
    }
    
    public void RemovePlayer(TelegramUser user)
    {
        lock (_players)
        {
            if (RolesAssigned) throw new BusinessException("Игра уже начинается.");
            var player = _players.FirstOrDefault(x => x.User.Id == user.Id);

            if (player == null) return;
            
            _players.Remove(player);
            player.User.SessionId = null;
        }
    }
    
    public void RemovePlayer(Player player) => RemovePlayer(player.User);

    public void SaveRoleSelection(List<string> roles)
    {
        roles.ForEach(r => _selectedRoles.Add(r));
    }
    
    public void AssignRolesToPlayers()
    {
        if (RolesAssigned) return;
        if (Players.Count == 0 || Players.Count != _selectedRoles.Count)
        {
            throw new BusinessException("Количество игроков не соответствует количеству ролей.");
        }

        var randomizedRoles = _selectedRoles.ToArray();
        Random.Shared.Shuffle(randomizedRoles);

        for (var i = 0; i < randomizedRoles.Length; i++)
        {
            Players[i].Role = randomizedRoles[i];
        }
        RolesAssigned = true;
    }

}
