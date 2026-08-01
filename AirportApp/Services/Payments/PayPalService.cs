using System;
using System.Collections.Generic;
using System.Globalization;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;
using Microsoft.Extensions.Options;
using AirportApp.Settings;

namespace AirportApp.Services.Payments
{
    public class PayPalOrderResult
    {
        public string OrderId { get; set; } = string.Empty;
        public string ApprovalUrl { get; set; } = string.Empty;
        public string RawResponse { get; set; } = string.Empty;
    }

    public class PayPalCaptureResult
    {
        public string Status { get; set; } = string.Empty;
        public string CaptureId { get; set; } = string.Empty;
        public string RawResponse { get; set; } = string.Empty;
    }

    public class PayPalService
    {
        private readonly HttpClient _httpClient;
        private readonly PayPalSettings _settings;

        public PayPalService(HttpClient httpClient, IOptions<PayPalSettings> options)
        {
            _httpClient = httpClient;
            _settings = options.Value;
        }

        private async Task<string> GetAccessTokenAsync()
        {
            var authHeaderValue = Convert.ToBase64String(Encoding.UTF8.GetBytes($"{_settings.ClientId}:{_settings.ClientSecret}"));
            
            var request = new HttpRequestMessage(HttpMethod.Post, $"{_settings.BaseUrl}/v1/oauth2/token");
            request.Headers.Authorization = new AuthenticationHeaderValue("Basic", authHeaderValue);
            request.Content = new FormUrlEncodedContent(new[]
            {
                new KeyValuePair<string, string>("grant_type", "client_credentials")
            });

            var response = await _httpClient.SendAsync(request);
            response.EnsureSuccessStatusCode();

            var json = await response.Content.ReadFromJsonAsync<JsonElement>();
            return json.GetProperty("access_token").GetString() ?? string.Empty;
        }

        public async Task<PayPalOrderResult> CreateOrderAsync(decimal total, string reference, string returnUrl = "", string cancelUrl = "")
        {
            string token = await GetAccessTokenAsync();

            string finalReturnUrl = string.IsNullOrEmpty(returnUrl) ? _settings.ReturnUrl : returnUrl;
            string finalCancelUrl = string.IsNullOrEmpty(cancelUrl) ? _settings.CancelUrl : cancelUrl;

            var orderRequest = new
            {
                intent = "CAPTURE",
                purchase_units = new[]
                {
                    new
                    {
                        reference_id = reference,
                        amount = new
                        {
                            currency_code = "USD",
                            value = total.ToString("F2", CultureInfo.InvariantCulture)
                        }
                    }
                },
                application_context = new
                {
                    return_url = finalReturnUrl,
                    cancel_url = finalCancelUrl,
                    user_action = "PAY_NOW"
                }
            };

            var request = new HttpRequestMessage(HttpMethod.Post, $"{_settings.BaseUrl}/v2/checkout/orders");
            request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", token);
            request.Content = JsonContent.Create(orderRequest);

            var response = await _httpClient.SendAsync(request);
            var rawJson = await response.Content.ReadAsStringAsync();
            response.EnsureSuccessStatusCode();

            var doc = JsonDocument.Parse(rawJson);
            var root = doc.RootElement;
            string orderId = root.GetProperty("id").GetString() ?? string.Empty;
            string approvalUrl = string.Empty;

            foreach (var link in root.GetProperty("links").EnumerateArray())
            {
                if (link.GetProperty("rel").GetString() == "approve")
                {
                    approvalUrl = link.GetProperty("href").GetString() ?? string.Empty;
                    break;
                }
            }

            return new PayPalOrderResult
            {
                OrderId = orderId,
                ApprovalUrl = approvalUrl,
                RawResponse = rawJson
            };
        }

        public async Task<PayPalCaptureResult> CaptureOrderAsync(string orderId)
        {
            string token = await GetAccessTokenAsync();

            var request = new HttpRequestMessage(HttpMethod.Post, $"{_settings.BaseUrl}/v2/checkout/orders/{orderId}/capture");
            request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", token);
            request.Content = new StringContent(string.Empty, Encoding.UTF8, "application/json");

            var response = await _httpClient.SendAsync(request);
            var rawJson = await response.Content.ReadAsStringAsync();
            response.EnsureSuccessStatusCode();

            var doc = JsonDocument.Parse(rawJson);
            var root = doc.RootElement;
            string status = root.GetProperty("status").GetString() ?? string.Empty;
            string captureId = string.Empty;

            if (root.TryGetProperty("purchase_units", out var puArray) && puArray.ValueKind == JsonValueKind.Array)
            {
                foreach (var pu in puArray.EnumerateArray())
                {
                    if (pu.TryGetProperty("payments", out var payments) && payments.TryGetProperty("captures", out var captures) && captures.ValueKind == JsonValueKind.Array)
                    {
                        foreach (var cap in captures.EnumerateArray())
                        {
                            captureId = cap.GetProperty("id").GetString() ?? string.Empty;
                            break;
                        }
                    }
                }
            }

            return new PayPalCaptureResult
            {
                Status = status,
                CaptureId = captureId,
                RawResponse = rawJson
            };
        }
    }
}
