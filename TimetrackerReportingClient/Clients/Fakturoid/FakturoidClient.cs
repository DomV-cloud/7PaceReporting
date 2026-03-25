using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using RestSharp;
using System.Text;
using TimetrackerReportingClient.Helpers;
using TimetrackerReportingClient.Models.Fakturoid;

namespace TimetrackerReportingClient.Clients.Fakturoid;

/// <summary>
/// Client for the Fakturoid API v3.
/// Uses OAuth2 Client Credentials flow.
///
/// Credentials are obtained from: Settings → User account → OAuth 2 credentials
/// Slug is visible in the URL when logged in: app.fakturoid.cz/api/v3/accounts/{slug}/
/// </summary>
public class FakturoidClient
{
    private const string BaseUrl = "https://app.fakturoid.cz/api/v3";
    private const string TokenUrl = "https://app.fakturoid.cz/api/v3/oauth/token";
    private const string AppName = "TimePaceInvoicer (pbarabas@example.com)";

    private readonly string _slug;
    private readonly string _clientId;
    private readonly string _clientSecret;

    private string _accessToken;

    public FakturoidClient(string slug, string clientId, string clientSecret)
    {
        Ensure.NotNullOrEmpty(slug, nameof(slug));
        Ensure.NotNullOrEmpty(clientId, nameof(clientId));
        Ensure.NotNullOrEmpty(clientSecret, nameof(clientSecret));

        _slug = slug;
        _clientId = clientId;
        _clientSecret = clientSecret;
    }

    /// <summary>
    /// Creates an invoice in Fakturoid and returns (invoiceId, invoiceNumber, locationUrl).
    /// </summary>
    public (int InvoiceId, string InvoiceNumber, string Location) CreateInvoice(
        InvoiceRequest invoice
    )
    {
        EnsureAccessToken();

        var client = new RestClient(BaseUrl);
        var request = new RestRequest($"accounts/{_slug}/invoices.json", Method.Post);

        request.AddHeader("Authorization", "Bearer " + _accessToken);
        request.AddHeader("User-Agent", AppName);

        var body = JsonConvert.SerializeObject(invoice);
        request.AddStringBody(body, ContentType.Json);

        var response = client.Execute(request);

        if (!response.IsSuccessful)
        {
            throw new Exception(
                $"Fakturoid invoice creation failed [{response.StatusCode}]: {response.Content}"
            );
        }

        var location = response
            .Headers.ToList()
            .Find(h => h.Name?.Equals("Location", StringComparison.OrdinalIgnoreCase) == true)
            ?.Value?.ToString();

        var responseJson = JObject.Parse(response.Content);
        var invoiceNumber = responseJson["number"]?.ToString();
        var invoiceId = responseJson["id"]?.Value<int>() ?? 0;

        Console.WriteLine($"  Invoice number: {invoiceNumber}");
        return (
            invoiceId,
            invoiceNumber,
            location ?? $"{BaseUrl}/accounts/{_slug}/invoices.json"
        );
    }

    /// <summary>
    /// Downloads the PDF for the given invoice ID and returns the raw bytes.
    /// Fakturoid generates PDFs asynchronously, so this method polls until
    /// the file is ready (up to <paramref name="timeoutSeconds"/> seconds).
    /// </summary>
    public byte[] DownloadInvoicePdf(int invoiceId, int timeoutSeconds = 30)
    {
        EnsureAccessToken();

        var deadline = DateTime.UtcNow.AddSeconds(timeoutSeconds);
        int attempt = 0;

        while (true)
        {
            attempt++;
            var client = new RestClient(BaseUrl);
            var request = new RestRequest(
                $"accounts/{_slug}/invoices/{invoiceId}/download.pdf",
                Method.Get
            );

            request.AddHeader("Authorization", "Bearer " + _accessToken);
            request.AddHeader("User-Agent", AppName);
            request.AddHeader("Accept", "application/pdf");

            var response = client.Execute(request);

            // 204 No Content means the PDF is not generated yet — wait and retry
            if (
                response.StatusCode == System.Net.HttpStatusCode.NoContent
                || (
                    response.IsSuccessful
                    && (response.RawBytes == null || response.RawBytes.Length == 0)
                )
            )
            {
                if (DateTime.UtcNow >= deadline)
                    throw new Exception(
                        $"Fakturoid PDF was not ready after {timeoutSeconds} s (invoice {invoiceId})."
                    );

                Console.WriteLine($"  PDF not ready yet (attempt {attempt}), retrying in 3 s…");
                System.Threading.Thread.Sleep(3000);
                continue;
            }

            if (!response.IsSuccessful)
                throw new Exception(
                    $"Fakturoid PDF download failed [{response.StatusCode}]: {response.Content}"
                );

            return response.RawBytes;
        }
    }

    private void EnsureAccessToken()
    {
        if (!string.IsNullOrEmpty(_accessToken))
            return;

        var client = new RestClient(TokenUrl);
        var request = new RestRequest(string.Empty, Method.Post);

        // Basic Auth: Base64(client_id:client_secret)
        var credentials = Convert.ToBase64String(
            Encoding.UTF8.GetBytes($"{_clientId}:{_clientSecret}")
        );

        request.AddHeader("Authorization", "Basic " + credentials);
        request.AddHeader("User-Agent", AppName);
        request.AddStringBody("{\"grant_type\":\"client_credentials\"}", ContentType.Json);

        var response = client.Execute(request);

        if (!response.IsSuccessful)
        {
            throw new Exception(
                $"Fakturoid authentication failed [{response.StatusCode}]: {response.Content}"
            );
        }

        var token = JsonConvert.DeserializeObject<TokenResponse>(response.Content);
        _accessToken =
            token?.AccessToken
            ?? throw new Exception("Fakturoid returned an empty access token.");
    }
}
