using Microsoft.Extensions.Hosting;
using Telegram.Bot;
using Telegram.Bot.Types;
using Werwolf_Bot.dto;
using Werwolf_Bot.services;
using Serilog;

var telegramApiKey = Environment
    .GetEnvironmentVariable("TELEGRAM_WERWOLF_API_KEY");

if (string.IsNullOrWhiteSpace(telegramApiKey))
{
    throw new InvalidOperationException("TELEGRAM_WERWOLF_API_KEY is not set.");
}

var userService = new UserService();
var botClient = new TelegramBotClient(telegramApiKey);
using var cts = new CancellationTokenSource();
SessionService sessionService = new SessionService();
var languageService = new LanguageService("ru");
var loc = new LocalizationService();
loc.LoadLanguage("ru");
ChatService chatService = new ChatService(
    sessionService, botClient, languageService, userService, cts.Token);

botClient.StartReceiving(
    updateHandler: HandleUpdateAsync,
    errorHandler: HandleErrorAsync,
    cancellationToken: cts.Token
);

var me = await botClient.GetMe();
Console.WriteLine($"Bot @{me.Username} has started.");

/* test create new Session
var testUser = new TelegramUser(123, "testUN", "testFN", "testLN");
var testPlayer = new Player(new TelegramUser(123, "testUN", "testFN", "testLN"), true);
var testSession = sessionService.CreateSession(testUser);
testSession.SaveRoleSelection(new List<string>{ "werwolf" });
Console.WriteLine("Session created.");
Console.WriteLine($"Session ID: {testSession.Id}");
var sessionPlayers =
    string.Join("\n", testSession.Players.Select(p => $"username: {p.User.Username}, id: {p.User.Id}"));
Console.WriteLine($"Session players: {sessionPlayers}");

await Task.Delay(50000);
await chatService.SendRoleCardsToPlayersAsync(testSession);
//end test */

// Holds Process active
await Task.Delay(-1, cts.Token);

cts.Cancel();

async Task HandleUpdateAsync(ITelegramBotClient bot,
    Update update,
    CancellationToken cancellationToken)
{
    if (update.Message == null) return;
    var user = userService.GetUser(update.Message.Chat);

    Console.WriteLine($"User: {user.Username}, Step: {user.Step}");
    Console.WriteLine("********");
    
    switch (user.Step)
    {
        case UserStep.None:
            await chatService.GetChooseLanguageScreen(user);
            break;
        case UserStep.ChooseLanguage:
            await chatService.GetChoosePlayModeScreen(update.Message, user);
            break;
        case UserStep.ChoosePlayMode:
            await chatService.GetHostPlayerLanguageScreen(update.Message, user);
            break;
        case UserStep.EnterSessionId when update.Message.Text is { } sessionId:
            await chatService.GetWaitingRoleScreen(user, sessionId);
            break;
        case UserStep.AwaitingRole:
            await chatService.GetLeaveSessionScreen(update.Message, user);
            break;
        case UserStep.ChooseRoles:
            await chatService.GetHostLobbyScreen(update, user);
            
            /* test: add user to session
            var testUser = new TelegramUser(123, "testUN", "testFN", "testLN");
            var testPlayer = new Player(testUser, false);
            var session = sessionService.GetGameSessionByHostId(user.Id);
            session!.AddPlayerToSession(testPlayer);
            //end test */
            
            break;
        case UserStep.WaitingPlayersToJoin:
            await chatService.StartOrCancelGame(update, user);
            break;
    }
}

Task HandleErrorAsync(ITelegramBotClient bot, Exception exception, CancellationToken cancellationToken)
{
    Console.WriteLine($"Error: {exception.Message}");
    return Task.CompletedTask;
}
