using System;

namespace TimetrackerReportingClient.Email
{
    /// <summary>
    /// Creates the correct <see cref="IEmailSender"/> based on the
    /// <c>emailProvider</c> value in appsettings.json.
    ///
    /// Supported values (case-insensitive):
    ///   "smtp"  — generic SMTP (Outlook / Office 365, Gmail, …)
    ///   "graph" — Microsoft Graph API (Outlook via Azure AD app registration)
    /// </summary>
    public static class EmailSenderFactory
    {
        public static IEmailSender Create(AppSettings settings)
        {
            var provider = (settings.EmailProvider ?? "smtp").Trim().ToLowerInvariant();

            switch (provider)
            {
                case "smtp":
                    if (string.IsNullOrEmpty(settings.SmtpHost))
                        throw new InvalidOperationException(
                            "emailProvider is 'smtp' but smtpHost is not set in appsettings.json."
                        );

                    return new SmtpEmailSender(
                        settings.SmtpHost,
                        settings.SmtpPort,
                        settings.SmtpUsername,
                        settings.SmtpPassword
                    );

                case "graph":
                    if (
                        string.IsNullOrEmpty(settings.GraphTenantId)
                        || string.IsNullOrEmpty(settings.GraphClientId)
                        || string.IsNullOrEmpty(settings.GraphClientSecret)
                    )
                    {
                        throw new InvalidOperationException(
                            "emailProvider is 'graph' but graphTenantId / graphClientId / graphClientSecret "
                                + "are not fully set in appsettings.json."
                        );
                    }

                    return new MicrosoftGraphEmailSender(
                        settings.GraphTenantId,
                        settings.GraphClientId,
                        settings.GraphClientSecret
                    );

                default:
                    throw new NotSupportedException(
                        $"Unknown emailProvider '{provider}'. Supported values: smtp, graph."
                    );
            }
        }
    }
}
