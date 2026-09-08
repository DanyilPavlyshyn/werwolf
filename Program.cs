using Telegram.Bot;
using Telegram.Bot.Types;
using Werwolf_Bot.dto;
using Werwolf_Bot.services;

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
UserStateService userStateService = new UserStateService();
var languageService = new LanguageService("ru");
var loc = new LocalizationService();
loc.LoadLanguage("ru");
ChatService chatService = new ChatService(
    sessionService, userStateService, botClient, languageService, cts.Token);

botClient.StartReceiving(
    updateHandler: HandleUpdateAsync,
    errorHandler: HandleErrorAsync,
    cancellationToken: cts.Token
);

var me = await botClient.GetMe();
Console.WriteLine($"Bot @{me.Username} has started.");

// Hält den Prozess dauerhaft aktiv, bis Systemd den Dienst stoppt (SIGTERM):
await Task.Delay(-1, cts.Token);

cts.Cancel();

async Task HandleUpdateAsync(ITelegramBotClient bot,
    Update update,
    CancellationToken cancellationToken)
{
    if (update.Message == null) return;
    var user = userService.GetUser(update.Message.Chat);
    
    var receiverId = update.Message.Chat.Id;
    var step = userStateService.GetStep(receiverId);

    Console.WriteLine($"User: {update.Message.Chat.Username}, Step: {step}");
    Console.WriteLine("********");
    
    switch (step)
    {
        case UserStep.None:
            await chatService.GetChoosePlayModeScreen(user);
            
            /* test as player
            var s = sessionService.CreateSession(receiverId);
            Console.WriteLine(s.Id);
            s.SaveRoleSelection(new List<string>() { "amo" });
            end test */ 
            
            break;
        case UserStep.ChoosePlayMode:
            await chatService.GetHostOrPlayerScreen(update.Message, receiverId);
            break;
        case UserStep.EnterSessionId when update.Message.Text is { } sessionId:
            await chatService.GetWaitingRoleScreen(update.Message, sessionId);
            break;
        case UserStep.ChooseRoles:
            await chatService.GetHostLobbyScreen(update, receiverId);
            
            /* test: Adding Players to Session */
            var session = sessionService.GetGameSessionByHostId(receiverId);
            session.AddPlayerToSession(
                new Player(123, "TestUser","Test", "User", false));
            session.AddPlayerToSession(
                new Player(124, "TestUser1","Test1", "User1", false));
            session.AddPlayerToSession(
                new Player(125, "TestUser2","Test2", "User2", false));
            //end test */
            
            break;
        case UserStep.WaitingPlayersToJoin:
            await chatService.StartOrCancelGame(update, receiverId);
            break;
    }
}

Task HandleErrorAsync(ITelegramBotClient bot, Exception exception, CancellationToken cancellationToken)
{
    Console.WriteLine($"Error: {exception.Message}");
    return Task.CompletedTask;
}
