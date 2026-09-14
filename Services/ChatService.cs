using System.Net;
using Telegram.Bot;
using Telegram.Bot.Types;
using Telegram.Bot.Types.Enums;
using Telegram.Bot.Types.ReplyMarkups;
using Werwolf_Bot.Models;

namespace Werwolf_Bot.Services;

public class ChatService(
    ITelegramBotClient bot,
    LocalizationService localization,
    UserService users,
    CancellationToken cancellationToken)
{
    private readonly ButtonsService buttons = new(localization);

    private string Text(TelegramUser user, string key, params object?[] arguments) =>
        localization.GetText(user.Language, key, arguments);

    private static string Html(string? value) => WebUtility.HtmlEncode(value ?? string.Empty);

    private TelegramUser Host(GameSession session) => users.FindUser(session.HostId)
        ?? throw new BusinessException("sessionNotFound");

    public Task GetStepResponse(TelegramUser user) => user.Step switch
    {
        UserStep.ChoosingLanguage => SendMessage(user, Text(user, "pleaseChooseLanguage"),
            buttons.GetChooseLanguageButtons(user.Language)),
        UserStep.ChoosingPlayMode => SendMessage(user, Text(user, "welcome"),
            buttons.GetChoosePlayModeButtons(user.Language)),
        UserStep.ChoosingRoles => SendMessage(user, Text(user, "pleaseChooseRoles"),
            buttons.GetChooseRolesButtons(user.Language)),
        UserStep.WaitingPlayersToJoin => SendMessage(user,
            Text(user, "shareSessionId", Html(user.SessionId?.ToUpperInvariant())),
            buttons.GetSessionCancelButtons(user.Language)),
        UserStep.EnteringSessionId => SendMessage(user, Text(user, "pleaseEnterId"),
            buttons.GetLeaveSessionButtons(user.Language)),
        UserStep.WaitingStart => SendMessage(user, Text(user, "sessionJoined"),
            buttons.GetLeaveSessionButtons(user.Language)),
        UserStep.CanceledAsRole => SendMessage(user, Text(user, "sessionLeft")),
        UserStep.CanceledAsHost => SendMessage(user, Text(user, "sessionCancelled")),
        UserStep.SessionCancelledByHost => SendMessage(user, Text(user, "sessionCancelledByHost")),
        _ => Task.CompletedTask
    };

    public async Task SendMessage(TelegramUser user, string message,
        ReplyMarkup? buttons = null, ParseMode? parseMode = ParseMode.Html)
    {
        await bot.SendMessage(chatId: user.Id, text: message,
            replyMarkup: buttons ?? new ReplyKeyboardRemove(),
            parseMode: parseMode ?? ParseMode.None, cancellationToken: cancellationToken);
    }

    public Task SendBusinessError(TelegramUser user, BusinessException error) =>
        SendMessage(user, Text(user, error.MessageKey, error.Arguments), parseMode: ParseMode.None);

    public Task SendSessionCancelledByHostToPlayers(List<Player> players) =>
        Task.WhenAll(players.Select(p =>
            SendMessage(p.User, Text(p.User, "sessionCancelledByHostNotification"))));

    public Task SendPlayersAndRolesToHostAsync(GameSession session)
    {
        var host = Host(session);
        var entries = session.Players.OrderBy(p => p.Role).Select(p => Text(host,
            "playerRoleListEntry", Html(localization.GetRole(host.Language, p.Role!).Title),
            Html(p.User.FirstName), Html(p.User.LastName), Html(p.User.Username)));
        return SendMessage(host, Text(host, "rolesAssigned", string.Join("\n", entries)));
    }

    public async Task SendRulesToHostAsync(GameSession session)
    {
        var host = Host(session);
        var roles = session.SelectedRoles.Select(key => localization.GetRole(host.Language, key))
            .OrderBy(role => role.NightPrio).ToList();
        var descriptions = roles.Select(role => Text(host, "roleDescriptionEntry",
            Html(role.Title), Html(role.Description)));
        await SendMessage(host, Text(host, "rolesDescription", string.Join("\n", descriptions)));

        string Order(IEnumerable<RoleInfo> selected) => string.Join("\n", selected
            .Where(role => role.NightPrio > 0)
            .Select((role, index) => Text(host, "numberedRoleEntry", index + 1, Html(role.Title))));
        await SendMessage(host, Text(host, "rolesOrder", Order(roles),
            Order(roles.Where(role => !role.OnlyFirstNight))));
    }

    public Task SendPlayerListToHostAsync(GameSession session)
    {
        var host = Host(session);
        if (session.Players.Count == session.SelectedRoles.Count)
        {
            var names = session.Players.Select((p, index) => Text(host, "numberedPlayerEntry",
                index + 1, Html(p.User.FirstName), Html(p.User.LastName), Html(p.User.Username)));
            var list = Text(host, "playersList", string.Join("\n", names));
            return SendMessage(host, Text(host, "sessionIsReady", list),
                buttons.GetSessionStartEndButtons(host.Language));
        }
        return SendMessage(host, Text(host, "playersCountUpdate", session.Players.Count,
            session.SelectedRoles.Count - session.Players.Count),
            buttons.GetSessionCancelButtons(host.Language));
    }

    public async Task SendRoleCardsToPlayersAsync(GameSession session)
    {
        session.AssignRolesToPlayers();
        foreach (var player in session.Players)
        {
            if (player.IsHost || player.CardDelivered || player.User.SessionId != session.Id) continue;
            var languageCode = LocalizationService.GetLanguageCode(player.User.Language);
            var path = Path.Combine(AppContext.BaseDirectory, "Assets", "Cards", languageCode, $"{player.Role}.png");
            if (!File.Exists(path))
                path = Path.Combine(AppContext.BaseDirectory, "Assets", "Cards", "ru", $"{player.Role}.png");
            // The existing Russian asset uses this legacy spelling.
            if (!File.Exists(path) && player.Role == "psychopath")
                path = Path.Combine(AppContext.BaseDirectory, "Assets", "Cards", "ru", "psyhopath.png");
            var role = localization.GetRole(player.User.Language, player.Role!);
            await using var stream = File.OpenRead(path);
            await bot.SendPhoto(chatId: player.User.Id,
                photo: InputFile.FromStream(stream, $"{player.Role}.png"),
                caption: Text(player.User, "yourRole", Html(role.Title)),
                parseMode: ParseMode.Html, replyMarkup: new ReplyKeyboardRemove(),
                cancellationToken: cancellationToken);
            player.CardDelivered = true;
        }
    }
}
