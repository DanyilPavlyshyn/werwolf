
using Telegram.Bot;
using Telegram.Bot.Exceptions;
using Telegram.Bot.Types;
using Werwolf_Bot.Models;
using Werwolf_Bot.Models.services;
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

        /*
        switch (user.Step)
        {
            case _UserStep.None:
                await chatService.GetChooseLanguageScreen(user);
                break;
            case _UserStep.ChooseLanguage:
                await chatService.SetLanguage(botUpdate, user);
                break;
            case _UserStep.LanguageChosed:
                await chatService.GetChoosePlayModeScreen(botUpdate, user);
                break;
            case _UserStep.ChoosePlayMode:
                await chatService.GetHostPlayerLanguageScreen(botUpdate, user);
                break;
            case _UserStep.EnterSessionId:
                await chatService.GetWaitingRoleScreen(user, botUpdate);
                break;
            case _UserStep.AwaitingRole:
                await chatService.GetLeaveSessionScreen(botUpdate, user);
                break;
            case _UserStep.ChooseRoles:
                await chatService.GetHostLobbyScreen(botUpdate, user);

                /* test: add and remove user to session
                var testUser = new TelegramUser(123, "testUN", "testFN", "testLN");
                var testPlayer = new Player(testUser, false);
                var session = sessionService.GetGameSessionByHostId(user.Id);

                await Task.Delay(2000);
                session!.AddPlayer(testPlayer);

                await Task.Delay(2000);
                session!.RemovePlayer(testPlayer);
                //end test

                break;
            case _UserStep.WaitingPlayersToJoin:
                await chatService.StartOrCancelGame(botUpdate, user);
                break;
        }
    */
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
