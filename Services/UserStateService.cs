using System.Collections.Concurrent;
using Werwolf_Bot.dto;
using Werwolf_Bot.services;

namespace Werwolf_Bot.services;

public class UserStateService
{
    private readonly ConcurrentDictionary<long, UserStep> _steps = new();

    public UserStep GetStep(long userId)
    {
        return _steps.GetValueOrDefault(userId, UserStep.None);
    }

    public void SetStep(long userId, UserStep step)
    {
        _steps[userId] = step;
    }

    public void ClearStep(long userId)
    {
        _steps.TryRemove(userId, out _);
    }

    public void SetStepForPlayers(List<Player> players, UserStep step)
    {
        foreach (var player in players)
        {
            SetStep(player.Id, step);
        }
    }
}