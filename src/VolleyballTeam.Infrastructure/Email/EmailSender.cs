using Azure;
using Azure.Communication.Email;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using VolleyballTeam.Infrastructure.Config;

namespace VolleyballTeam.Infrastructure.Email;

public record EmailMessage(string To, string Subject, string Html, string PlainText);

public partial class EmailSender(IOptions<EmailOptions> options, Lazy<EmailClient> client, ILogger<EmailSender> logger)
{
    public async Task<bool> SendAsync(EmailMessage message, CancellationToken cancellationToken = default)
    {
        var settings = options.Value;

        if (!settings.IsConfigured)
        {
            LogNotConfigured(logger, message.To, message.Subject);
            if (settings.LogBodyWhenNotConfigured)
            {
                var singleLineBody = message.PlainText.ReplaceLineEndings(" ");
                LogBody(logger, singleLineBody);
            }

            return false;
        }

        try
        {
            // Started, not Completed: delivery can take minutes and the request shouldn't wait for it.
            await client.Value.SendAsync(
                WaitUntil.Started, settings.SenderAddress, message.To, message.Subject, message.Html, message.PlainText, cancellationToken);
            return true;
        }
        catch (RequestFailedException ex)
        {
            LogFailed(logger, ex, message.To, message.Subject);
            return false;
        }
    }

    [LoggerMessage(Level = LogLevel.Warning, Message = "Email is not configured; not sending \"{Subject}\" to {To}.")]
    private static partial void LogNotConfigured(ILogger logger, string to, string subject);

    [LoggerMessage(Level = LogLevel.Information, Message = "Unsent email body: {Body}")]
    private static partial void LogBody(ILogger logger, string body);

    [LoggerMessage(Level = LogLevel.Error, Message = "Sending \"{Subject}\" to {To} failed.")]
    private static partial void LogFailed(ILogger logger, Exception exception, string to, string subject);
}
