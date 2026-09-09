
using Microsoft.Extensions.Logging;
using Telegram.Bot;
using Telegram.Bot.Polling;
using Telegram.Bot.Types;
using Telegram.Bot.Types.Enums;

namespace Werwolf_Bot.services;

public class BotUpdateHandler
{
    private readonly ILogger<BotUpdateHandler> _logger;

    public BotUpdateHandler(ILogger<BotUpdateHandler> logger)
    {
        _logger = logger;
    }

    public async Task HandleLoggingAsync(
        ITelegramBotClient botClient, Update update, 
        CancellationToken cancellationToken)
    {
        if (update.Type == UpdateType.Message && update.Message is { } message)
        {
            LogIncomingMessage(message);
        }
        else if (update.Type == UpdateType.CallbackQuery 
                 && update.CallbackQuery is { } callbackQuery)
        {
            _logger.LogInformation(
                "[Callback] From: {UserId} ({Username}) | Data: {Data}",
                callbackQuery.From.Id,
                callbackQuery.From.Username ?? "NoUsername",
                callbackQuery.Data
            );
        }
    }

    private void LogIncomingMessage(Message message)
    {
        var chatId = message.Chat.Id;
        var userId = message.From?.Id ?? 0;
        var username = message.From?.Username ?? message.From?.FirstName ?? "Unknown";

        var textContent = message.Type switch
        {
            MessageType.Text => message.Text,
            MessageType.Photo => "[Photo]",
            MessageType.Sticker => $"[Sticker: {message.Sticker?.Emoji}]",
            MessageType.Document => $"[Document: {message.Document?.FileName}]",
            MessageType.Voice => "[Voice Note]",
            _ => $"[{message.Type}]"
        };
        
        _logger.LogInformation(
            "[Message] ChatId: {ChatId} | User: {UserId} ({Username}) | Text: {Text}",
            chatId,
            userId,
            username,
            textContent
        );
    }
}