using Telegram.Bot.Types;
using Telegram.Bot.Types.ReplyMarkups;

namespace Werwolf_Bot.services;

public static class ButtonsService 
{
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
            }
        })
        {
            ResizeKeyboard = true, 
            OneTimeKeyboard = true 
        };
    }
    
    public static ReplyKeyboardMarkup GetChooseRolesButtons()
    {
        return new ReplyKeyboardMarkup(KeyboardButton.WithWebApp(
            text: "🐺 Выбор ролей",
            webApp: new WebAppInfo { Url = "https://danyilpavlyshyn.github.io/werwolf/Assets/Pages/roles.html" }
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
}
