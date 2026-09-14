using System.Text.Json;
using Werwolf_Bot.Models;

namespace Werwolf_Bot.Services;

public static class UserStepDispatcher
{
    public static void UpdateHostStep(TelegramUser host, GameSession session)
    {
        host.SetStep(session.Players.Count == session.SelectedRoles.Count && session.Players.Count > 0
            ? UserStep.ReadyToStart
            : UserStep.WaitingPlayersToJoin);
    }

    public static void SetActualStep (
        TelegramUser user, BotUpdate message, 
        StepResult stepResult)
    {
        switch (user.Step)
        {
            case UserStep.None:
                user.SetStep(user.Language == null 
                    ? UserStep.ChoosingLanguage 
                    : UserStep.ChoosingPlayMode);
                break;
            case UserStep.ChoosingLanguage:
                if (stepResult == StepResult.LanguagedChanged)
                    user.SetStep(UserStep.ChoosingPlayMode);
                break;
            case UserStep.ChoosingPlayMode:
                switch (stepResult)
                {
                    case StepResult.HostModeSelected:
                        user.SetStep(UserStep.ChoosingRoles);
                        break;
                    case StepResult.PlayerModeSelected:
                        user.SetStep(UserStep.EnteringSessionId);
                        break;
                    case StepResult.ChangeLanguageSelected:
                        user.SetStep(UserStep.ChoosingLanguage);
                        break;
                }
                break;
            case UserStep.ChoosingRoles:
                switch (stepResult)
                {
                    case StepResult.RolesChosenByHost:
                        user.SetStep(UserStep.WaitingPlayersToJoin);
                        break;
                }
                break;
            case UserStep.WaitingPlayersToJoin:
                if (stepResult is StepResult.SessionCanceled)
                    user.SetStep(UserStep.CanceledAsHost);
                break;
            case UserStep.ReadyToStart:
                switch (stepResult)
                {
                    case StepResult.GameStarted:
                        user.SetStep(UserStep.StartedAsHost);
                        break;
                    case StepResult.SessionCanceled:
                        user.SetStep(UserStep.CanceledAsHost);
                        break;
                }
                break;
            case UserStep.EnteringSessionId:
                switch (stepResult)
                {
                    case StepResult.LeavedSession:
                        user.SetStep(UserStep.CanceledAsRole);
                        break;
                    case StepResult.SessionJoined:
                        user.SetStep(UserStep.WaitingStart);
                        break;
                }
                break;
            case UserStep.WaitingStart:
                if (stepResult is StepResult.LeavedSession)
                    user.SetStep(UserStep.CanceledAsRole);
                break;
            default:
                user.SetStep(user.Language == null 
                    ? UserStep.ChoosingLanguage 
                    : UserStep.ChoosingPlayMode);
                break;
        }
    }
}