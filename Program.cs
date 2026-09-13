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

// ToDo: lokalization for en, de, ua
var languageService = new LanguageService("ru");
var localizationService = new LocalizationService();

localizationService.LoadLanguage("ru");
ChatService chatService = new ChatService(
    sessionService, botClient, languageService,
    localizationService, userService, cts.Token);
StepHandler stepHandler = new StepHandler(
    sessionService, localizationService, chatService, userService);

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

    Console.WriteLine($"User: {user.Username}, Step: {user.Step}");
    Console.WriteLine("********");
    
    try
    {
        await stepHandler.HandleAsync(user, botUpdate);
        UserStepDispatcher.SetActualStep(user, botUpdate);
        await chatService.GetStepResponse(user);
    }
    catch (BusinessException exception)
    {
        await chatService.SendMessage(user, 
            exception.Message);
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
