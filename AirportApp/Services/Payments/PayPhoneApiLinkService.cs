using System;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json;
using System.Threading.Tasks;
using Microsoft.Extensions.Options;
using AirportApp.Settings;

namespace AirportApp.Services.Payments
{
    public class PayPhoneApiLinkService
    {
        private readonly HttpClient _httpClient;
        private readonly PayPhoneSettings _settings;

        public PayPhoneApiLinkService(HttpClient httpClient, IOptions<PayPhoneSettings> options)
        {
            _httpClient = httpClient;
            _settings = options.Value;
        }

        public async Task<string> CreatePaymentLinkAsync(decimal total, string clientTransactionId, string reference)
        {
            int amountInCents = (int)Math.Round(total * 100, MidpointRounding.AwayFromZero);

            var requestBody = new
            {
                amount = amountInCents,
                amountWithoutTax = amountInCents,
                amountWithTax = 0,
                tax = 0,
                service = 0,
                tip = 0,
                currency = "USD",
                reference = reference,
                clientTransactionId = clientTransactionId
            };

            var request = new HttpRequestMessage(HttpMethod.Post, "https://pay.payphonetodoesposible.com/api/Links");
            request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", _settings.Token);
            request.Content = JsonContent.Create(requestBody);

            var response = await _httpClient.SendAsync(request);
            response.EnsureSuccessStatusCode();

            var paymentUrl = await response.Content.ReadFromJsonAsync<string>();
            return paymentUrl ?? string.Empty;
        }
    }
}
