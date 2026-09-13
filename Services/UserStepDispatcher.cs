using System.Collections.Concurrent;
using System.Text.Json;
using Werwolf_Bot.Models;

namespace Werwolf_Bot.Services;

public static class UserStepDispatcher
{
    public static void SetActualStep (TelegramUser user, BotUpdate message)
    {
        switch (user.Step)
        {
            case UserStep.None:
                user.SetStep(user.Language == null 
                    ? UserStep.ChoosingLanguage 
                    : UserStep.ChoosingPlayMode);
                break;
            case UserStep.ChoosingLanguage:
                user.SetStep(user.Language == null 
                    ? UserStep.ChoosingLanguage 
                    : UserStep.ChoosingPlayMode);
                break;
            case UserStep.ChoosingPlayMode:
                switch (message)
                {
                    // buttons: Host, Player, ChangeLanguage
                    // PlayModeOrLanguageChangeInputHandler
                    case { Text: "Хочу быть ведущим 📝" }:
                        user.SetStep(UserStep.ChoosingRoles);
                        break;
                    case { Text: "Хочу играть 🐺" }:
                        user.SetStep(UserStep.EnteringSessionId);
                        break;
                    case { Text: "Change language 🌍" }:
                        user.SetStep(UserStep.ChoosingLanguage);
                        break;
                }
                break;
            case UserStep.ChoosingRoles:
                if (message.WebData is { } data)
                {
                    var result = JsonSerializer
                        .Deserialize<RolesChoice>(data);

                    if (result?.action == "confirmRoles")
                    {
                        user.SetStep(UserStep.WaitingPlayersToJoin);
                    }
                    else
                    {
                        user.SetStep(UserStep.ErrorByChoosingRoles);
                    }
                }
                break;
            case UserStep.WaitingPlayersToJoin:
                if (message is { Text: "Отменить игру ❌" })
                {
                    user.SetStep(UserStep.CanceledAsHost);
                }
                break;
            case UserStep.ReadyToStart:
                switch (message)
                {
                    case { Text: "Раздать карты 🃏" }:
                        user.SetStep(UserStep.StartedAsHost);
                        break;
                    case { Text: "Отменить игру ❌" }:
                        user.SetStep(UserStep.CanceledAsHost);
                        break;
                }
                break;
            case UserStep.EnteringSessionId:
                if (message is { Text: "Покинуть игру ❌" })
                {
                    user.SetStep(UserStep.CanceledAsRole);
                }
                break;
            case UserStep.WaitingStart:
                if (message is { Text: "Покинуть игру ❌" })
                {
                    user.SetStep(UserStep.CanceledAsRole);
                }
                break;
            default:
                user.SetStep(user.Language == null 
                    ? UserStep.ChoosingLanguage 
                    : UserStep.ChoosingPlayMode);
                break;
        }
    }
}