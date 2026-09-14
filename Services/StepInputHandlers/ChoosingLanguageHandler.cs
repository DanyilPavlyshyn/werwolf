using Werwolf_Bot.Models;

namespace Werwolf_Bot.Services.StepInputHandlers;

public class ChoosingLanguageHandler
{
    public StepResult GetResult(
        TelegramUser user, 
        BotUpdate update,
        LocalizationService localization)
    {
        if (update.Text == null) return  StepResult.NoChange;
        var language = localization.GetUserLanguage(update.Text, user.Language);
        if (language == null) return StepResult.NoChange; 

        user.SetLanguage(language.Value);
        return StepResult.LanguagedChanged;
    }
}