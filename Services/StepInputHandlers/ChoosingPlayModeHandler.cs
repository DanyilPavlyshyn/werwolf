using Telegram.Bot.Types;
using Werwolf_Bot.Models;

namespace Werwolf_Bot.Services.StepInputHandlers;

public class ChoosingPlayModeHandler()
{
    public StepResult GetResult(BotUpdate update)
    {
        switch (update)
        {
            case { Text: "Хочу быть ведущим 📝" }:
                return StepResult.HostModeSelected;
            case { Text: "Хочу играть 🐺" }:
                return StepResult.PlayerModeSelected;
            case { Text: "Change language 🌍" }:
                return StepResult.ChangeLanguageSelected;
        }
        return StepResult.NoChange;
    }
}