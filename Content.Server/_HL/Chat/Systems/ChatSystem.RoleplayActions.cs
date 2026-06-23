// #Cythisiax Added - RP action formatting and .do command support, ported from nuclear-14
using Content.Shared.Chat;
using Content.Shared.Database;
using Robust.Shared.Console;
using Robust.Shared.Player;
using Robust.Shared.Utility;

namespace Content.Server.Chat.Systems;

public sealed partial class ChatSystem
{
    /// <summary>
    ///     Wraps action text in italic markup for RP chat messages.
    /// </summary>
    public string FormatRoleplayActionMarkup(string action)
    {
        return FormattedMessage.EscapeText(action);
    }

    /// <summary>
    ///     Builds the wrapped message for a .do environmental action.
    ///     Uses the existing <c>chat-manager-entity-do-wrap-message</c> locale string.
    /// </summary>
    public string BuildDoWrappedMessage(string message)
    {
        return Loc.GetString("chat-manager-entity-do-wrap-message", ("message", message));
    }

    /// <summary>
    ///     Sends a .do-style environmental action message — italic, nameless, no speech bubble.
    ///     The message appears to every player in normal voice range of the source.
    /// </summary>
    public void TrySendInGameDoMessage(
        EntityUid source,
        string action,
        ChatTransmitRange range,
        bool hideLog,
        IConsoleShell? shell = null,
        ICommonSession? player = null)
    {
        if (string.IsNullOrWhiteSpace(action))
            return;

        // Sanitize emote shorthands
        if (_sanitizer.TrySanitizeEmoteShorthands(action, source, out var sanitized, out _))
            action = sanitized;

        if (player != null)
        {
            _chatManager.EnsurePlayer(player.UserId).AddEntity(GetNetEntity(source));
        }

        var formatted = FormatRoleplayActionMarkup(action);
        var wrappedMessage = BuildDoWrappedMessage(formatted);

        // Send to everyone in voice range with EntityUid.Invalid to suppress speech bubble
        foreach (var (session, data) in GetRecipients(source, VoiceRange))
        {
            var entRange = MessageRangeCheck(session, data, range);
            if (entRange == MessageRangeCheckResult.Disallowed)
                continue;

            var entHideChat = entRange == MessageRangeCheckResult.HideChat;

            _chatManager.ChatMessageToOne(
                ChatChannel.Emotes,
                action,
                wrappedMessage,
                EntityUid.Invalid,
                entHideChat,
                session.Channel,
                author: player?.UserId);
        }

        _replay.RecordServerMessage(
            new ChatMessage(ChatChannel.Emotes, action, wrappedMessage, default, null, false));

        if (!hideLog && player != null)
        {
            _adminLogger.Add(
                LogType.Chat,
                LogImpact.Low,
                $".do from {player:Player}: {action}");
        }
    }
}
