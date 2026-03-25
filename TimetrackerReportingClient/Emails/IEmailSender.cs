namespace TimetrackerReportingClient.Email
{
    /// <summary>
    /// Abstraction over email providers so the call site is the same
    /// regardless of whether you use SMTP (Outlook, Gmail …) or the
    /// Microsoft Graph API.
    /// </summary>
    public interface IEmailSender
    {
        /// <summary>
        /// Sends an invoice PDF as an email attachment.
        /// </summary>
        /// <param name="invoiceNumber">Human-readable invoice number used in the subject line.</param>
        /// <param name="from">Sender address.</param>
        /// <param name="to">Recipient address.</param>
        /// <param name="pdfBytes">Raw PDF bytes to attach.</param>
        void SendInvoice(string invoiceNumber, string from, string to, byte[] pdfBytes);
    }
}
