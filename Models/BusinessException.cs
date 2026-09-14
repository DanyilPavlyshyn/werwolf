namespace Werwolf_Bot.Models;

// Domain code reports a key; the chat layer translates it for the recipient.
public class BusinessException : Exception
{
    public string MessageKey { get; }
    public object?[] Arguments { get; }

    public BusinessException(string messageKey, params object?[] arguments) : base(messageKey)
    {
        MessageKey = messageKey;
        Arguments = arguments;
    }

    public BusinessException(string messageKey, Exception innerException) : base(messageKey, innerException)
    {
        MessageKey = messageKey;
        Arguments = Array.Empty<object?>();
    }
}
