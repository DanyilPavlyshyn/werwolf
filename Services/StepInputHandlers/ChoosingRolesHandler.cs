using System.Text.Json;
using Werwolf_Bot.Models;

namespace Werwolf_Bot.Services.StepInputHandlers;

public class ChoosingRolesHandler
{
    public StepResult GetResult(
        TelegramUser user,
        BotUpdate update,
        SessionService sessions,
        LocalizationService localization)
    {
        if (update.WebData == null) return StepResult.NoChange;
        RolesChoice? choice;
        try { choice = JsonSerializer.Deserialize<RolesChoice>(update.WebData); }
        catch (JsonException)
        {
            throw new BusinessException("invalidRoleSelection");
        }
        if (choice?.action != "confirmRoles") return StepResult.NoChange;
        if (choice.roles == null || choice.roles.Count == 0 ||
            choice.roles.Any(role => string.IsNullOrWhiteSpace(role) ||
                localization.GetRole("ru", role) == null))
            throw new BusinessException("pleaseSelectAtLeastOneRole");

        var session = sessions.CreateSession(user);
        session.SaveRoleSelection(choice.roles);
        return StepResult.RolesChosenByHost;
    }
}
