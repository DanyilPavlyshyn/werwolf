using Werwolf_Bot.Models;

namespace Werwolf_Bot.Services.StepInputHandlers;

public class ChoosingPlayModeHandler(LocalizationService localization)
{
    public StepResult GetResult(TelegramUser user, BotUpdate update)
    {
        if (localization.Matches(user.Language, update.Text, "button.chooseHost")) return StepResult.HostModeSelected;
        if (localization.Matches(user.Language, update.Text, "button.choosePlayer")) return StepResult.PlayerModeSelected;
        if (localization.Matches(user.Language, update.Text, "button.changeLanguage")) return StepResult.ChangeLanguageSelected;
        return StepResult.NoChange;
    }
}
