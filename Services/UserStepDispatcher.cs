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
                // buttons for langs
                // languageInputHandler
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
                // webForm for Roles choice
                // webFormDataInputHandler
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
                // button to Cancel Session
                // cancelSessionInputHandler
                // if handler.success => user.SetStep(UserStep.ChoosingPlayMode);
                break;
            case UserStep.ReadyToStart:
                // buttons Start and Cancel session
                // StartCancelInputHandler
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
                // button to Exit Session
                // SessionIdInputHandler
                // if handler.success => user.SetStep(UserStep.WaitingStart);
                // if handler.error => no changes
                if (message is { Text: "Покинуть игру ❌" })
                {
                    user.SetStep(UserStep.CanceledAsRole);
                }
                break;
            case UserStep.WaitingStart:
                // button to Exit Session
                if (message is { Text: "Покинуть игру ❌" })
                {
                    user.SetStep(UserStep.CanceledAsRole);
                }
                break;
            default:
                user.SetStep(UserStep.None);
                break;
        }
    }
}