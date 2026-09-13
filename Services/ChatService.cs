using System.Text;
using System.Text.Json;
using Telegram.Bot;
using Telegram.Bot.Types;
using Telegram.Bot.Types.Enums;
using Telegram.Bot.Types.ReplyMarkups;
using Werwolf_Bot.Models;

namespace Werwolf_Bot.Services;

//<summary>
// Service class for chat communication actions
//</summary>
public class ChatService(
    SessionService sessionService,
    ITelegramBotClient bot,
    LanguageService localService,
    LocalizationService localizationService,
    UserService userService,
    CancellationToken cancellationToken)
{
    public async Task SendMessage(
        TelegramUser user,
        string message, 
        ReplyMarkup? buttons = null,
        ParseMode? parseMode = ParseMode.Html)
    {
        await bot.SendMessage(
            chatId: user.Id,
            text: message,
            replyMarkup: buttons ?? new ReplyKeyboardRemove(),
            cancellationToken: cancellationToken
        );
    }
    
    public async Task GetChooseLanguageScreen(TelegramUser user)
    {
        user.SetStep(UserStep.ChoosingLanguage);
        await SendMessage(
            user, 
            "Hi! Please choose your language:", 
            ButtonsService.GetChooseLanguageButtons());
    }
    public async Task SetLanguage(BotUpdate message, TelegramUser user)
    {
        if (message.Text is null) return;
        
        var language = localizationService.GetUserLanguage(message.Text);
        
        if (language == null) return;
        
        user.SetLanguage(language.Value);
        user.SetStep(UserStep.ChoosingPlayMode);
        await GetChoosePlayModeScreen(message, user);
    }
    public async Task GetChoosePlayModeScreen(BotUpdate message, TelegramUser user)
    {
        user.SetStep(UserStep.ChoosingPlayMode);
        await SendMessage(
            user,
            "Привет! Хочешь играть или вести игру?",
            ButtonsService.GetChoosePlayModeButtons()
        );
        
    }

    public async Task GetHostPlayerLanguageScreen(BotUpdate message, TelegramUser user)
    {
        if (message is { Text: "Хочу быть ведущим 📝" })
        {
            user.SetStep(UserStep.ChoosingRoles);
            await SendMessage(
                user,
                "Отлично, теперь нужно выбрать роли. Количество ролей должно соответствовать количеству игроков.",
                ButtonsService.GetChooseRolesButtons(user.Language)
            );
        }
        else if (message is { Text: "Хочу играть 🐺" })
        {
            user.SetStep(UserStep.EnteringSessionId);
            await bot.SendMessage(
                chatId: user.Id,
                text: "Хорошо, если введущий уже создал игру и сообщил тебе id, отправь мне его в чате:",
                cancellationToken: cancellationToken
            );
        }
        else if (message is { Text: "Change language 🌍" })
        {
            user.SetStep(UserStep.ChoosingLanguage);
            await bot.SendMessage(
                chatId: user.Id,
                text: "Hi! Please choose your language:",
                replyMarkup: ButtonsService.GetChooseLanguageButtons(),
                cancellationToken: cancellationToken
            );
        }
    }

    public async Task GetWaitingRoleScreen(TelegramUser user, BotUpdate update)
    {
            var session = sessionService.GetSession(update.Text);
            if (session is null) throw new BusinessException("Ошибка Id! Проверь правильность Id и введи еще раз.");
            
            var player = new Player(user, false);
            session.AddPlayer(player);
            user.SetStep(UserStep.WaitingStart);
            await SendMessage(
                user,
                "Подключено! Теперь ожидай начала игры и получения своей роли.",
                ButtonsService.GetLeaveSessionButtons());
    }

    public async Task GetLeaveSessionScreen(BotUpdate message, TelegramUser user)
    {
        if (message is { Text: "Покинуть игру ❌" })
        {
            var gameSession = sessionService.GetSession(user.SessionId);
            gameSession?.RemovePlayer(user);
            
            user.SetStep(UserStep.None);
            await SendMessage(
                user,
                "Отключено, пиши, если захочешь поиграть. :)"
            );
        }
    }

    public async Task GetHostLobbyScreen(BotUpdate update, TelegramUser user)
    {
        var gameSession = sessionService.CreateSession(user);
        
        if (update.WebData is { } data)
        {
            var result = JsonSerializer
                .Deserialize<RolesChoice>(data);

            if (result?.action == "confirmRoles")
            {
                gameSession.SaveRoleSelection(result.roles);
                user.SetStep(UserStep.WaitingPlayersToJoin);
                gameSession.AddPlayersObserver(async (_, updatedPlayers) =>
                {
                    await SendPlayerListToHostAsync(gameSession);
                });
                
                await SendMessage(
                    user,
                    $"Роли выбраны, теперь сообщи Id игрокам и ожидай их подключения. ID:<blockquote>{gameSession.Id.ToUpper()}</blockquote>",
                    ButtonsService.GetSessionCancelButtons()
                );
            }
        }
    }

    public async Task StartOrCancelGame(BotUpdate update, TelegramUser user)
    {
        var gameSession = sessionService.GetSession(user.SessionId);
        
        if (gameSession == null) 
        {
            user.SetStep(UserStep.None);
            throw new BusinessException("Произошла ошибка. Создай новую игру или присоеденись.");
        }

        switch (update)
        {
            case { Text: "Раздать карты 🃏" }:
                user.SetStep(UserStep.StartedAsHost);
                await SendRoleCardsToPlayersAsync(gameSession);
                await SendPlayersAndRolesToHostAsync(gameSession);
                await SendRulesToHostAsync(gameSession);
                break;
            case { Text: "Отменить игру ❌" }:
                await SendMessage(
                    user,
                    "Игровая сессия отменена. \n\n Захочешь еще поиграть - пиши. :)"
                );
                break;
        }
        
        // setting state to default and deleting Session to free space
        userService.SetStepForUsers(gameSession.Players, UserStep.ChoosingLanguage);
        user.SetStep(UserStep.ChoosingLanguage);
        user.SessionId = null;
        sessionService.DeleteSession(gameSession);
    }

    public async Task SendPlayersAndRolesToHostAsync(GameSession gameSession)
    {
        StringBuilder rolePlayerList = new StringBuilder();
        
        gameSession.Players
            .OrderBy(player => player.Role)
            .ToList()
            .ForEach(player => rolePlayerList.AppendLine(
                $"{localService.GetRole(player.Role).Title} - {player.User.FirstName} {player.User.LastName}, @{player.User.Username}"));
        
        await bot.SendMessage(
            chatId: gameSession.HostId,
            text: $"Роли разданы:\n\n{rolePlayerList}",
            replyMarkup: new ReplyKeyboardRemove(),
            cancellationToken: cancellationToken
        );
    }

    public async Task SendRulesToHostAsync(GameSession gameSession)
    {
        StringBuilder roleDescriptions = new StringBuilder();
        StringBuilder rulesFirstNight = new StringBuilder();
        StringBuilder rulesAllNights = new StringBuilder();
        
        var roleObjects = gameSession.SelectedRoles
            .Select(role => localService.GetRole(role))
            .OrderBy(r => r.NightPrio)
            .ToList();
        roleObjects.ForEach(role => roleDescriptions.AppendLine($"<b>{role.Title}</b>: {role.Description}"));
            
        await bot.SendMessage(
            chatId: gameSession.HostId,
            text: $"<blockquote><b>Описание ролей</b>:\n{roleDescriptions}</blockquote>",
            parseMode: ParseMode.Html,
            cancellationToken: cancellationToken
        );
            
        for (int i = 0; i < roleObjects.Count; i++)
        {
            if (roleObjects[i] == null) continue;
            if (roleObjects[i] is { NightPrio: 0 }) continue;
            
            rulesFirstNight.AppendLine($"{ i+1 }. { roleObjects[i].Title }");
        }
        
        var rolesFromSecondNight = roleObjects
            .FindAll(role => role.OnlyFirstNight == false )
            .OrderBy(r => r.NightPrio)
            .ToList();
            
        for (int j = 0; j < rolesFromSecondNight.Count(); j++)
        {
            if (rolesFromSecondNight[j] == null) continue;
            if (rolesFromSecondNight[j] is { NightPrio: 0 }) continue;
            
            rulesAllNights.AppendLine($"{ j+1 }. { rolesFromSecondNight[j].Title }");
        }
        
        await bot.SendMessage(
            chatId: gameSession.HostId,
            text: $"Называемые роли первой ночи:\n{rulesFirstNight}\nНазываемые роли со второй ночи:\n{rulesAllNights}\nХорошей игры! :)",
            cancellationToken: cancellationToken
        );
    }

    public async Task SendPlayerListToHostAsync(GameSession gameSession)
    {
        if (gameSession.Players.Count == gameSession.SelectedRoles.Count)
        {
            var playerNames = gameSession.Players
                .Select((p, i) => $"{i + 1}. {p.User.FirstName} {p.User.LastName}, @{p.User.Username}").ToList();
            string playersList = $"Cписок игроков:\n\n{string.Join("\n", playerNames)}";
            
            await bot.SendMessage(
                chatId: gameSession.HostId,
                text: $"{playersList}\n\nНеобходимое количествово игроков подключено, можешь раздавать карты. Хорошей игры!",
                replyMarkup: ButtonsService.GetSessionStartEndButtons(),
                cancellationToken: cancellationToken
            );
        }
        else
        {
            int playerCount = gameSession.Players.Count;
            int playersNeeded = gameSession.SelectedRoles.Count - playerCount;
            
            await bot.SendMessage(
                chatId: gameSession.HostId,
                text: $"Всего игроков: {playerCount}. Для начала игры необходимо еще: {playersNeeded}",
                parseMode: Telegram.Bot.Types.Enums.ParseMode.Markdown,
                replyMarkup: ButtonsService.GetSessionCancelButtons(),
                cancellationToken: cancellationToken
            );
        }
    }

    public async Task SendRoleCardsToPlayersAsync(GameSession gameSession)
    {
        gameSession.AssignRolesToPlayers();
        
        foreach (var player in gameSession.Players)
        {
            if (!player.IsHost && player.User.SessionId == gameSession.Id)
            {
                var filePath = $"Assets/Cards/ru/{player.Role}.png";
                await using FileStream stream = System.IO.File.OpenRead(filePath);
                await bot.SendPhoto(
                    chatId: player.User.Id,
                    photo: InputFile.FromStream(stream, $"{player.Role}.png"),
                    caption: $"Твоя роль - <b>{localService.GetRole(player.Role).Title}</b>!\nОзнакомся с деталями на карточке.\nХорошей игры! :)",
                    parseMode: ParseMode.Html,
                    replyMarkup: new ReplyKeyboardRemove()
                );
                player.User.SetStep(UserStep.None);
            }
        }
    }
}
