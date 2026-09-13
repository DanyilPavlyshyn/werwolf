using Telegram.Bot.Types;
using Telegram.Bot.Types.ReplyMarkups;
using Werwolf_Bot.Models;

namespace Werwolf_Bot.Services;

public static class ButtonsService 
{
    private static Dictionary<UserLanguage, string> _langs = new()
    {
        [UserLanguage.English] = "en",
        [UserLanguage.German] = "de",
        [UserLanguage.Ukrainian] = "uk",
        [UserLanguage.Russian] = "ru"
    };
    
    public static ReplyKeyboardMarkup GetChooseLanguageButtons()
    {
        return new ReplyKeyboardMarkup(new[]
        {
            new[]
            {
                new KeyboardButton("EN 🇬🇧"),
                new KeyboardButton("DE 🇩🇪")
            },
            new[]
            {
                new KeyboardButton("UA 🇺🇦"),
                new KeyboardButton("RU 🇷🇺")
            }
        })
        {
            ResizeKeyboard = true, 
            OneTimeKeyboard = true 
        };
    }
    
    public static ReplyKeyboardMarkup GetChoosePlayModeButtons()
    {
        return new ReplyKeyboardMarkup(new[]
        {
            new[]
            {
                new KeyboardButton("Хочу быть ведущим 📝")
            },
            new[]
            {
                new KeyboardButton("Хочу играть 🐺")
            },
            new[]
            {
                new KeyboardButton("Change language 🌍")
            }
        })
        {
            ResizeKeyboard = true, 
            OneTimeKeyboard = true 
        };
    }
    
    public static ReplyKeyboardMarkup GetChooseRolesButtons(UserLanguage? language)
    {
        var langCode = _langs[language ?? UserLanguage.English];
        
        return new ReplyKeyboardMarkup(KeyboardButton.WithWebApp(
            text: "🐺 Выбор ролей",
            webApp: new WebAppInfo { Url = $"https://danyilpavlyshyn.github.io/werwolf/Assets/Pages/roles.html?language={langCode}" }
        ))
        {
            ResizeKeyboard = true,
            OneTimeKeyboard = true
        };
    }

    public static ReplyKeyboardMarkup GetSessionStartEndButtons()
    {
        return new ReplyKeyboardMarkup(new[]
        {
            new[]
            {
                new KeyboardButton("Раздать карты 🃏")
            },
            new[]
            {
                new KeyboardButton("Отменить игру ❌")
            }
        })
        {
            ResizeKeyboard = true, 
            OneTimeKeyboard = true 
        };
    }

    public static ReplyKeyboardMarkup GetSessionCancelButtons()
    {
        return new ReplyKeyboardMarkup(new[]
        {
            new[]
            {
                new KeyboardButton("Отменить игру ❌")
            }
        })
        {
            ResizeKeyboard = true, 
            OneTimeKeyboard = true 
        };
    }
    
    public static ReplyKeyboardMarkup GetLeaveSessionButtons()
    {
        return new ReplyKeyboardMarkup(new[]
        {
            new[]
            {
                new KeyboardButton("Покинуть игру ❌")
            }
        })
        {
            ResizeKeyboard = true, 
            OneTimeKeyboard = true 
        };
    }
}
