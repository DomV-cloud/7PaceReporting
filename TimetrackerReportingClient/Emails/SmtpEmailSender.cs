using System;
using System.Net;
using System.Net.Mail;

namespace TimetrackerReportingClient.Email
{
    /// <summary>
    /// Sends email via any standard SMTP server.
    ///
    /// Outlook / Office 365  →  host: smtp.office365.com, port: 587
    /// Gmail                 →  host: smtp.gmail.com,     port: 587
    ///
    /// Both providers require an App Password (or OAuth device-code token)
    /// when multi-factor authentication is enabled on the account.
    /// </summary>
    public class SmtpEmailSender : IEmailSender
    {
        private readonly string _host;
        private readonly int _port;
        private readonly string _username;
        private readonly string _password;

        public SmtpEmailSender(string host, int port, string username, string password)
        {
            _host = host;
            _port = port;
            _username = username;
            _password = password;
        }

        public void SendInvoice(string invoiceNumber, string from, string to, byte[] pdfBytes)
        {
            using (var message = new MailMessage())
            {
                message.From = new MailAddress(from);
                message.To.Add(new MailAddress(to));
                message.Subject = $"Invoice {invoiceNumber}";
                message.Body = $"Please find attached invoice {invoiceNumber}.\r\n\r\nBest regards";

                var attachment = new Attachment(
                    new System.IO.MemoryStream(pdfBytes),
                    $"invoice_{invoiceNumber}.pdf",
                    "application/pdf"
                );
                message.Attachments.Add(attachment);

                using (var smtp = new SmtpClient(_host, _port))
                {
                    smtp.EnableSsl = true;
                    smtp.Credentials = new NetworkCredential(_username, _password);

                    try
                    {
                        smtp.Send(message);
                    }
                    catch (SmtpException ex)
                        when (ex.Message.Contains("5.7.139")
                            || ex.Message.Contains("basic authentication is disabled")
                            || ex.Message.Contains("Authentication unsuccessful")
                        )
                    {
                        throw new InvalidOperationException(
                            "Outlook/Office 365 has disabled basic (username+password) SMTP authentication.\n"
                                + "Switch to the Microsoft Graph provider instead:\n"
                                + "  1. Set \"emailProvider\": \"graph\" in appsettings.json\n"
                                + "  2. Fill in graphTenantId, graphClientId, graphClientSecret\n"
                                + "  3. Grant Mail.Send application permission in Azure AD\n"
                                + "See the previous setup instructions for details.",
                            ex
                        );
                    }
                }
            }
        }
    }
}
