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
