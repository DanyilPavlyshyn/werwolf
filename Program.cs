using Telegram.Bot;
using Telegram.Bot.Types;
using Werwolf_Bot.Models;
using Werwolf_Bot.Services;

var telegramApiKey = Environment
    .GetEnvironmentVariable("TELEGRAM_WERWOLF_API_KEY");

if (string.IsNullOrWhiteSpace(telegramApiKey))
{
    throw new InvalidOperationException("TELEGRAM_WERWOLF_API_KEY is not set.");
}
// ToDo: inversion Type Principle - Telegram Entities into Dto

var userService = new UserService();
var botClient = new TelegramBotClient(telegramApiKey);
using var cts = new CancellationTokenSource();
SessionService sessionService = new SessionService();

var localizationService = new LocalizationService();

ChatService chatService = new ChatService(
    botClient, localizationService, userService, cts.Token);
StepHandler stepHandler = new StepHandler(
    sessionService, localizationService, chatService, userService);

var updateProcessor = new UpdateProcessor(stepHandler, sessionService, userService, chatService);

botClient.StartReceiving(
    updateHandler: HandleUpdateAsync,
    errorHandler: HandleErrorAsync,
    cancellationToken: cts.Token
);

var me = await botClient.GetMe();
Console.WriteLine($"Bot @{me.Username} has started.");
await Task.Delay(-1, cts.Token);

cts.Cancel();

async Task HandleUpdateAsync(ITelegramBotClient bot,
    Update update,
    CancellationToken cancellationToken)
{
    if (update.Message == null) return;
    var user = userService.GetUser(update.Message.Chat);
    var botUpdate = new BotUpdate(
        update.Message.Text,
        update.Message.WebAppData?.Data
    );
    
    try
    {
        await updateProcessor.HandleAsync(user, botUpdate);
    }
    catch (BusinessException exception)
    {
        await chatService.SendBusinessError(user, exception);
    }

    // ToDo: logging StepHandler
}

Task HandleErrorAsync(
    ITelegramBotClient bot,
    Exception exception,
    CancellationToken cancellationToken)
{
    // ToDo: logging Errors
    Console.WriteLine($"Error: {exception.Message}");
    return Task.CompletedTask;
}
