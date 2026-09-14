using Telegram.Bot.Types;
using Telegram.Bot.Types.ReplyMarkups;
using Werwolf_Bot.Models;

namespace Werwolf_Bot.Services;

public class ButtonsService(LocalizationService localization)
{
    private KeyboardButton Button(UserLanguage? language, string key) => new(localization.GetText(language, key));

    private static ReplyKeyboardMarkup Keyboard(IEnumerable<IEnumerable<KeyboardButton>> rows) => new(rows)
    {
        ResizeKeyboard = true,
        OneTimeKeyboard = true
    };

    private ReplyKeyboardMarkup Rows(UserLanguage? language, params string[] keys) =>
        Keyboard(keys.Select(key => new[] { Button(language, key) }));

    public ReplyKeyboardMarkup GetChooseLanguageButtons(UserLanguage? language) => Keyboard(new[]
    {
        new[] { Button(language, "button.chooseEnglish"), Button(language, "button.chooseGerman") },
        new[] { Button(language, "button.chooseUkrainian"), Button(language, "button.chooseRussian") }
    });

    public ReplyKeyboardMarkup GetChoosePlayModeButtons(UserLanguage? language) =>
        Rows(language, "button.chooseHost", "button.choosePlayer", "button.changeLanguage");

    public ReplyKeyboardMarkup GetChooseRolesButtons(UserLanguage? language) => Keyboard(new[]
    {
        new[] { KeyboardButton.WithWebApp(
            text: localization.GetText(language, "button.chooseRoles"),
            webApp: new WebAppInfo
            {
                Url = $"https://danyilpavlyshyn.github.io/werwolf/Assets/Pages/roles.html?language={LocalizationService.GetLanguageCode(language)}"
            }) }
    });

    public ReplyKeyboardMarkup GetSessionStartEndButtons(UserLanguage? language) =>
        Rows(language, "button.dealCards", "button.cancelSession");

    public ReplyKeyboardMarkup GetSessionCancelButtons(UserLanguage? language) =>
        Rows(language, "button.cancelSession");

    public ReplyKeyboardMarkup GetLeaveSessionButtons(UserLanguage? language) =>
        Rows(language, "button.leaveSession");
}
