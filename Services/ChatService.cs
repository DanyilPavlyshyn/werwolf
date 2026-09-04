using System.Text;
using System.Text.Json;
using Telegram.Bot;
using Telegram.Bot.Types;
using Telegram.Bot.Types.Enums;
using Werwolf_Bot.dto;

namespace Werwolf_Bot.services;

//<summary>
// Service class for chat communication actions
//</summary>
public class ChatService(
    SessionService sessionService,
    UserStateService userStateService,
    ITelegramBotClient bot,
    LanguageService localService,
    CancellationToken cancellationToken)
{
    public async Task GetChoosePlayModeScreen(long receiverId)
    {
        userStateService.SetStep(receiverId, UserStep.ChoosePlayMode);
        await bot.SendMessage(
            chatId: receiverId,
            text: "Привет! Хочешь играть или вести игру?",
            replyMarkup: ButtonsService.GetChoosePlayModeButtons(),
            cancellationToken: cancellationToken
        );
    }

    public async Task GetHostOrPlayerScreen(Message message, long receiverId)
    {
        if (message is { Text: "Хочу быть ведущим 📝" })
        {
            userStateService.SetStep(receiverId, UserStep.ChooseRoles);
            await bot.SendMessage(
                chatId: message.Chat.Id,
                text: "Отлично, теперь нужно выбрать роли. Количество ролей должно соответствовать количеству игроков.",
                replyMarkup: ButtonsService.GetChooseRolesButtons(),
                cancellationToken: cancellationToken
            );
        }
        else if (message is { Text: "Хочу играть 🐺" })
        {
            userStateService.SetStep(receiverId, UserStep.EnterSessionId);
            await bot.SendMessage(
                chatId: message.Chat.Id,
                text: "Хорошо, если введущий уже создал игру и сообщил тебе id, отправь мне его в чате:",
                cancellationToken: cancellationToken
            );
        }
    }

    public async Task GetWaitingRoleScreen(Message message, string sessionId)
    {
        try
        {
            var player = new Player(
                message.Chat.Id,
                message.Chat.Username,
                message.Chat.FirstName,
                message.Chat.LastName,
                false);

            sessionService.JoinSession(sessionId, player);
            // no Step defined
            await bot.SendMessage(
                chatId: message.Chat.Id,
                text: "Подключено! Теперь ожидай начала игры и получения своей роли.",
                cancellationToken: cancellationToken
            );
            Console.WriteLine("Session joined!");
        }
        catch (Exception e)
        {
            Console.WriteLine(e);
            await bot.SendMessage(
                chatId: message.Chat.Id,
                text: e.Message,
                cancellationToken: cancellationToken
            );
        }
    }

    public async Task GetHostLobbyScreen(Update update, long hostId)
    {
        var gameSession = sessionService.CreateSession(hostId);
        
        if (update.Message.WebAppData.Data is { } data)
        {
            var result = JsonSerializer
                .Deserialize<GameSession.RolesChoice>(data);

            if (result?.action == "confirmRoles")
            {
                gameSession.SaveRoleSelection(result.roles);
                userStateService.SetStep(hostId, UserStep.WaitingPlayersToJoin);
                gameSession.AddPlayersObserver(async (_, updatedPlayers) =>
                {
                    await SendPlayerListToHostAsync(gameSession);
                });
                
                await bot.SendMessage(
                    chatId: hostId,
                    text: $"Отлично, роли выбраны, теперь сообщи Id игрокам и ожидай их подключения. ID:<blockquote>{gameSession.Id.ToUpper()}</blockquote>",
                    parseMode: ParseMode.Html,
                    replyMarkup: ButtonsService.GetSessionCancelButtons(),
                    cancellationToken: cancellationToken
                );
            }
        }
    }

    public async Task StartOrCancelGame(Update update, long hostId)
    {
        var gameSession = sessionService.GetGameSessionByHostId(hostId);
        
        if (gameSession == null)
        {
            await bot.SendMessage(
                chatId: hostId,
                text: "Игровая сессия не найдена. Создай новую или присоеденись.",
                replyMarkup: ButtonsService.GetChoosePlayModeButtons(),
                cancellationToken: cancellationToken
            );
            userStateService.ClearStep(hostId);
            return;
        }

        if (update.Message is { Text: "Раздать карты 🃏" })
        {
            userStateService.SetStep(hostId, UserStep.GameStarted);
            await SendRoleCardsToPlayersAsync(gameSession);
            await SendPlayersAndRolesToHostAsync(gameSession);
            await SendRulesToHostAsync(gameSession);
        }
        else if (update.Message is { Text: "Отменить игру ❌" })
        {
            await bot.SendMessage(
                chatId: hostId,
                text: "Игровая сессия отменена. \n\n Захочешь еще поиграть - пиши. :)",
                cancellationToken: cancellationToken
            );
        }
        
        // setting state to default and deleting Session to free space
        userStateService.SetStepForPlayers(gameSession.Players, UserStep.None);
        userStateService.SetStep(hostId, UserStep.None);
        sessionService.DeleteSession(gameSession);
    }

    private async Task SendPlayersAndRolesToHostAsync(GameSession gameSession)
    {
        StringBuilder rolePlayerList = new StringBuilder();
        
        gameSession.Players
            .ForEach(player => rolePlayerList.AppendLine(
                $"{player.Role} - {player.FirstName} {player.LastName}, @{player.Username}"));
        
        await bot.SendMessage(
            chatId: gameSession.HostId,
            text: $"Роли разданы:\n\n{rolePlayerList}",
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
            rulesFirstNight.AppendLine($"{ i+1 }. { roleObjects[i].Title }");
        }
        
        var rolesFromSecondNight = roleObjects
            .FindAll(role => role.OnlyFirstNight == false )
            .OrderBy(r => r.NightPrio)
            .ToList();
            
        for (int j = 0; j < rolesFromSecondNight.Count(); j++)
        {
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
                .Select((p, i) => $"{i + 1}. {p.FirstName} {p.LastName}, @{p.Username}").ToList();
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
                cancellationToken: cancellationToken
            );
        }
    }

    private async Task SendRoleCardsToPlayersAsync(GameSession gameSession)
    {
        gameSession.AssignRolesToPlayers();
        
        foreach (var player in gameSession.Players)
        {
            if (!player.IsHost && player.SessionId == gameSession.Id)
            {
                var filePath = $"Assets/Cards/ru/{player.Role}.png";
                await using FileStream stream = System.IO.File.OpenRead(filePath);
                await bot.SendPhoto(
                    chatId: gameSession.HostId, // after test change to player.Id,
                    photo: InputFile.FromStream(stream, $"{player.Role}.png"),
                    caption: $"Твоя роль - <b>{localService.GetRole(player.Role).Title}</b>!\nОзнакомся с деталями на карточке.\nХорошей игры! :)",
                    parseMode: ParseMode.Html
                );
            }
        }
    }
}
