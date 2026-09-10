using System.Text;
using System.Text.Json;
using Telegram.Bot;
using Telegram.Bot.Types;
using Telegram.Bot.Types.Enums;
using Telegram.Bot.Types.ReplyMarkups;
using Werwolf_Bot.dto;

namespace Werwolf_Bot.services;

//<summary>
// Service class for chat communication actions
//</summary>
public class ChatService(
    SessionService sessionService,
    ITelegramBotClient bot,
    LanguageService localService,
    UserService userService,
    CancellationToken cancellationToken)
{
    public async Task GetChooseLanguageScreen(TelegramUser user)
    {
        user.SetStep(UserStep.ChooseLanguage);
        await bot.SendMessage(
            chatId: user.Id,
            text: "Hi! Please choose your language:",
            replyMarkup: ButtonsService.GetChooseLanguageButtons(),
            cancellationToken: cancellationToken
        );
    }
    public async Task GetChoosePlayModeScreen(Message message, TelegramUser user)
    {
        if (message.Text != null) user.SetLanguage(message.Text);

        user.SetStep(UserStep.ChoosePlayMode);
            await bot.SendMessage(
                chatId: user.Id,
                text: "Привет! Хочешь играть или вести игру?",
                replyMarkup: ButtonsService.GetChoosePlayModeButtons(),
                cancellationToken: cancellationToken
            );
    }

    public async Task GetHostPlayerLanguageScreen(Message message, TelegramUser user)
    {
        if (message is { Text: "Хочу быть ведущим 📝" })
        {
            user.SetStep(UserStep.ChooseRoles);
            await bot.SendMessage(
                chatId: user.Id,
                text: "Отлично, теперь нужно выбрать роли. Количество ролей должно соответствовать количеству игроков.",
                replyMarkup: ButtonsService.GetChooseRolesButtons(),
                cancellationToken: cancellationToken
            );
        }
        else if (message is { Text: "Хочу играть 🐺" })
        {
            user.SetStep(UserStep.EnterSessionId);
            await bot.SendMessage(
                chatId: message.Chat.Id,
                text: "Хорошо, если введущий уже создал игру и сообщил тебе id, отправь мне его в чате:",
                cancellationToken: cancellationToken
            );
        }
        else if (message is { Text: "Change language 🌍" })
        {
            user.SetStep(UserStep.ChooseLanguage);
            await bot.SendMessage(
                chatId: user.Id,
                text: "Hi! Please choose your language:",
                replyMarkup: ButtonsService.GetChooseLanguageButtons(),
                cancellationToken: cancellationToken
            );
        }
    }

    public async Task GetWaitingRoleScreen(TelegramUser user, string sessionId)
    {
            var player = new Player(user, false);
            var session = sessionService.JoinSession(sessionId, player);
            if (session != null)
            {            
                user.SetStep(UserStep.AwaitingRole);
                await bot.SendMessage(
                    chatId: user.Id,
                    text: "Подключено! Теперь ожидай начала игры и получения своей роли.",
                    replyMarkup: ButtonsService.GetLeaveSessionButtons(),
                    cancellationToken: cancellationToken
                );
            }
            else
            {
                user.SetStep(UserStep.ChoosePlayMode);
                await bot.SendMessage(
                    chatId: user.Id,
                    text: "Ошибка Id! Проверь правильность Id и введи еще раз.",
                    replyMarkup: ButtonsService.GetChoosePlayModeButtons(),
                    cancellationToken: cancellationToken
                );
            }

    }

    public async Task GetLeaveSessionScreen(Message message, TelegramUser user)
    {
        if (message is { Text: "Покинуть игру ❌" })
        {
            var gameSession = sessionService.GetSession(user.SessionId);
            
            if (gameSession != null)
            {
                gameSession.RemovePlayer(user);
            }
            
            user.SetStep(UserStep.ChooseLanguage);
            await bot.SendMessage(
                chatId: user.Id,
                text: "Отключено, пиши, если захочешь поиграть. :)",
                replyMarkup: new ReplyKeyboardRemove(),
                cancellationToken: cancellationToken
            );
        }
    }

    public async Task GetHostLobbyScreen(Update update, TelegramUser user)
    {
        var gameSession = sessionService.CreateSession(user);
        
        if (update.Message.WebAppData.Data is { } data)
        {
            var result = JsonSerializer
                .Deserialize<GameSession.RolesChoice>(data);

            if (result?.action == "confirmRoles")
            {
                gameSession.SaveRoleSelection(result.roles);
                user.SetStep(UserStep.WaitingPlayersToJoin);
                gameSession.AddPlayersObserver(async (_, updatedPlayers) =>
                {
                    await SendPlayerListToHostAsync(gameSession);
                });
                
                await bot.SendMessage(
                    chatId: user.Id,
                    text: $"Отлично, роли выбраны, теперь сообщи Id игрокам и ожидай их подключения. ID:<blockquote>{gameSession.Id.ToUpper()}</blockquote>",
                    parseMode: ParseMode.Html,
                    replyMarkup: ButtonsService.GetSessionCancelButtons(),
                    cancellationToken: cancellationToken
                );
            }
        }
    }

    public async Task StartOrCancelGame(Update update, TelegramUser user)
    {
        var gameSession = sessionService.GetSession(user.SessionId);
        if (gameSession == null)
        {
            await bot.SendMessage(
                chatId: user.Id,
                text: "Игровая сессия не найдена. Создай новую или присоеденись.",
                replyMarkup: ButtonsService.GetChoosePlayModeButtons(),
                cancellationToken: cancellationToken
            );
            user.SetStep(UserStep.ChooseLanguage);
            return;
        }

        switch (update.Message)
        {
            case { Text: "Раздать карты 🃏" }:
                user.SetStep(UserStep.GameStarted);
                await SendRoleCardsToPlayersAsync(gameSession);
                await SendPlayersAndRolesToHostAsync(gameSession);
                await SendRulesToHostAsync(gameSession);
                break;
            case { Text: "Отменить игру ❌" }:
                await bot.SendMessage(
                    chatId: user.Id,
                    text: "Игровая сессия отменена. \n\n Захочешь еще поиграть - пиши. :)",
                    replyMarkup: new ReplyKeyboardRemove(),
                    cancellationToken: cancellationToken
                );
                break;
        }
        
        // setting state to default and deleting Session to free space
        userService.SetStepForUsers(gameSession.Players, UserStep.ChooseLanguage);
        user.SetStep(UserStep.ChooseLanguage);
        sessionService.DeleteSession(gameSession);
    }

    private async Task SendPlayersAndRolesToHostAsync(GameSession gameSession)
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

    private async Task SendRulesToHostAsync(GameSession gameSession)
    {
        StringBuilder roleDescriptions = new StringBuilder();
        StringBuilder rulesFirstNight = new StringBuilder();
        StringBuilder rulesAllNights = new StringBuilder();
        
        List<string> roles = gameSession.GetSelectedRoles();
        var roleObjects = roles
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

    private async Task SendPlayerListToHostAsync(GameSession gameSession)
    {
        if (gameSession.Players.Count == gameSession.GetSelectedRoles().Count)
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
            int playersNeeded = gameSession.GetSelectedRoles().Count - playerCount;
            
            await bot.SendMessage(
                chatId: gameSession.HostId,
                text: $"Всего игроков: {playerCount}. Для начала игры необходимо еще: {playersNeeded}",
                parseMode: Telegram.Bot.Types.Enums.ParseMode.Markdown,
                replyMarkup: ButtonsService.GetSessionCancelButtons(),
                cancellationToken: cancellationToken
            );
        }
    }

    private async Task SendRoleCardsToPlayersAsync(GameSession gameSession)
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
                    parseMode: ParseMode.Html
                );
                player.User.Step = UserStep.ChooseLanguage;
            }
        }
    }
}
