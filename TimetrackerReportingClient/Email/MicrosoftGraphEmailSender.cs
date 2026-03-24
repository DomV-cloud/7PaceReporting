using System;
using System.IO;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using RestSharp;

namespace TimetrackerReportingClient.Email
{
    /// <summary>
    /// Sends email via Microsoft Graph API using OAuth2 Device Code flow.
    /// Works with personal Microsoft accounts (@outlook.com / @outlook.cz).
    ///
    /// Azure AD app registration requirements:
    ///   1. Supported account types: Personal Microsoft accounts (or Any + Personal)
    ///   2. Authentication → Allow public client flows: YES
    ///   3. API permissions → Microsoft Graph → Delegated → Mail.Send (grant consent)
    /// </summary>
    public class MicrosoftGraphEmailSender : IEmailSender
    {
        private const string GraphBaseUrl = "https://graph.microsoft.com/v1.0";
        private const string TokenEndpoint = "https://login.microsoftonline.com/consumers/oauth2/v2.0/token";
        private const string DeviceCodeEndpoint = "https://login.microsoftonline.com/consumers/oauth2/v2.0/devicecode";
        private const string Scope = "Mail.Send offline_access";

        private readonly string _clientId;
        private readonly string _tokenCachePath;

        private string _accessToken;

        public MicrosoftGraphEmailSender(string tenantId, string clientId, string clientSecret)
        {
            _clientId = clientId;

            // Store refresh token next to the exe so it persists across runs
            var exeDir = Path.GetDirectoryName(System.Reflection.Assembly.GetExecutingAssembly().Location);
            _tokenCachePath = Path.Combine(exeDir, "graph_token_cache.json");
        }

        public void SendInvoice(string invoiceNumber, string from, string to, byte[] pdfBytes)
        {
            EnsureAccessToken();

            var client  = new RestClient(GraphBaseUrl);
            var request = new RestRequest($"users/{Uri.EscapeDataString(from)}/sendMail", Method.POST);

            request.AddHeader("Authorization", "Bearer " + _accessToken);
            request.AddHeader("Content-Type", "application/json");

            var body = new JObject(
                new JProperty("message", new JObject(
                    new JProperty("subject", $"Invoice {invoiceNumber}"),
                    new JProperty("body", new JObject(
                        new JProperty("contentType", "Text"),
                        new JProperty("content", $"Please find attached invoice {invoiceNumber}.\r\n\r\nBest regards")
                    )),
                    new JProperty("toRecipients", new JArray(
                        new JObject(new JProperty("emailAddress", new JObject(new JProperty("address", to))))
                    )),
                    new JProperty("attachments", new JArray(
                        new JObject(
                            new JProperty("@odata.type", "#microsoft.graph.fileAttachment"),
                            new JProperty("name", $"invoice_{invoiceNumber}.pdf"),
                            new JProperty("contentType", "application/pdf"),
                            new JProperty("contentBytes", Convert.ToBase64String(pdfBytes))
                        )
                    ))
                )),
                new JProperty("saveToSentItems", true)
            );

            request.AddParameter("application/json", body.ToString(Formatting.None), ParameterType.RequestBody);

            var response = client.Execute(request);

            if (!response.IsSuccessful && response.StatusCode != System.Net.HttpStatusCode.Accepted)
            {
                throw new Exception($"Microsoft Graph sendMail failed [{response.StatusCode}]: {response.Content}");
            }
        }

        private void EnsureAccessToken()
        {
            // Try refresh token from cache first
            if (TryRefreshFromCache())
                return;

            // Fall back to interactive device code flow
            DoDeviceCodeFlow();
        }

        private bool TryRefreshFromCache()
        {
            if (!File.Exists(_tokenCachePath))
                return false;

            try
            {
                var cache        = JObject.Parse(File.ReadAllText(_tokenCachePath));
                var refreshToken = cache["refresh_token"]?.ToString();

                if (string.IsNullOrEmpty(refreshToken))
                    return false;

                var client  = new RestClient(TokenEndpoint);
                var request = new RestRequest(Method.POST);
                request.AddHeader("Content-Type", "application/x-www-form-urlencoded");
                request.AddParameter("grant_type",    "refresh_token");
                request.AddParameter("client_id",     _clientId);
                request.AddParameter("refresh_token", refreshToken);
                request.AddParameter("scope",         Scope);

                var response = client.Execute(request);
                if (!response.IsSuccessful)
                    return false;

                var json = JObject.Parse(response.Content);
                _accessToken = json["access_token"]?.ToString();
                if (string.IsNullOrEmpty(_accessToken))
                    return false;

                SaveTokenCache(json);
                return true;
            }
            catch
            {
                return false;
            }
        }

        private void DoDeviceCodeFlow()
        {
            // Step 1: request device code
            var client  = new RestClient(DeviceCodeEndpoint);
            var request = new RestRequest(Method.POST);
            request.AddHeader("Content-Type", "application/x-www-form-urlencoded");
            request.AddParameter("client_id", _clientId);
            request.AddParameter("scope",     Scope);

            var response = client.Execute(request);
            if (!response.IsSuccessful)
                throw new Exception($"Device code request failed [{response.StatusCode}]: {response.Content}");

            var json         = JObject.Parse(response.Content);
            var deviceCode   = json["device_code"]?.ToString();
            var userCode     = json["user_code"]?.ToString();
            var verifyUri    = json["verification_uri"]?.ToString();
            var expiresIn    = json["expires_in"]?.Value<int>() ?? 900;
            var intervalSec  = json["interval"]?.Value<int>() ?? 5;

            Console.WriteLine();
            Console.WriteLine("  ── Outlook authentication required ──────────────────");
            Console.WriteLine($"  1. Open: {verifyUri}");
            Console.WriteLine($"  2. Enter code: {userCode}");
            Console.WriteLine("  3. Sign in with your Outlook account");
            Console.WriteLine("  Waiting...");

            // Step 2: poll until user authenticates
            var deadline = DateTime.UtcNow.AddSeconds(expiresIn);
            while (DateTime.UtcNow < deadline)
            {
                System.Threading.Thread.Sleep(intervalSec * 1000);

                var pollClient  = new RestClient(TokenEndpoint);
                var pollRequest = new RestRequest(Method.POST);
                pollRequest.AddHeader("Content-Type", "application/x-www-form-urlencoded");
                pollRequest.AddParameter("grant_type",  "urn:ietf:params:oauth:grant-type:device_code");
                pollRequest.AddParameter("client_id",   _clientId);
                pollRequest.AddParameter("device_code", deviceCode);

                var pollResponse = pollClient.Execute(pollRequest);
                if (!pollResponse.IsSuccessful)
                {
                    var err = JObject.Parse(pollResponse.Content)["error"]?.ToString();
                    if (err == "authorization_pending") continue;
                    if (err == "slow_down") { intervalSec += 5; continue; }
                    throw new Exception($"Device code poll failed: {pollResponse.Content}");
                }

                var tokenJson = JObject.Parse(pollResponse.Content);
                _accessToken  = tokenJson["access_token"]?.ToString();

                if (string.IsNullOrEmpty(_accessToken))
                    throw new Exception("Graph returned an empty access token.");

                SaveTokenCache(tokenJson);
                Console.WriteLine("  Authenticated successfully.");
                return;
            }

            throw new Exception("Device code authentication timed out.");
        }

        private void SaveTokenCache(JObject tokenJson)
        {
            try { File.WriteAllText(_tokenCachePath, tokenJson.ToString(Formatting.Indented)); }
            catch { /* non-critical */ }
        }
    }
}
