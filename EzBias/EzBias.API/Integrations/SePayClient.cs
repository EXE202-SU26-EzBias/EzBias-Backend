using System.Globalization;
using System.Net;
using System.Net.Http.Headers;
using System.Text.Json;
using EzBias.Application.Features.Payments;
using Microsoft.Extensions.Options;

namespace EzBias.API.Integrations;

public class SePayClient : ISePayClient
{
    private readonly IHttpClientFactory _httpClientFactory;
    private readonly SePayOptions _options;

    public SePayClient(IHttpClientFactory httpClientFactory, IOptions<SePayOptions> options)
    {
        _httpClientFactory = httpClientFactory;
        _options = options.Value;
    }

    public async Task<(bool Success, string? Error, IReadOnlyList<SePayTransaction> Transactions, int? RetryAfterSeconds)> GetTransactionsAsync(string accountNumber, int limit, CancellationToken ct)
    {
        if (string.IsNullOrWhiteSpace(_options.ApiToken)) return (false, "SePay token missing.", Array.Empty<SePayTransaction>(), null);

        var client = _httpClientFactory.CreateClient("SePay");
        var url = $"/userapi/transactions/list?account_number={Uri.EscapeDataString(accountNumber)}&limit={limit}";
        using var req = new HttpRequestMessage(HttpMethod.Get, url);
        req.Headers.Authorization = new AuthenticationHeaderValue("Bearer", _options.ApiToken);

        using var res = await client.SendAsync(req, ct);
        if (res.StatusCode == (HttpStatusCode)429)
        {
            int? retry = null;
            if (res.Headers.TryGetValues("x-sepay-userapi-retry-after", out var vals) && int.TryParse(vals.FirstOrDefault(), out var s)) retry = s;
            return (false, "SePay rate limit exceeded.", Array.Empty<SePayTransaction>(), retry);
        }

        if (!res.IsSuccessStatusCode) return (false, $"SePay HTTP {(int)res.StatusCode}", Array.Empty<SePayTransaction>(), null);

        var json = await res.Content.ReadAsStringAsync(ct);
        using var doc = JsonDocument.Parse(json);
        var list = new List<SePayTransaction>();
        if (!doc.RootElement.TryGetProperty("transactions", out var arr) || arr.ValueKind != JsonValueKind.Array) return (true, null, list, null);

        foreach (var x in arr.EnumerateArray())
        {
            var id = x.TryGetProperty("id", out var idEl) ? (idEl.GetString() ?? string.Empty) : string.Empty;
            var amountInStr = x.TryGetProperty("amount_in", out var ai) ? ai.GetString() : "0";
            _ = decimal.TryParse(amountInStr, NumberStyles.Any, CultureInfo.InvariantCulture, out var amountIn);
            var content = x.TryGetProperty("transaction_content", out var tc) ? (tc.GetString() ?? string.Empty) : string.Empty;
            var refNo = x.TryGetProperty("reference_number", out var rn) ? rn.GetString() : null;
            var acc = x.TryGetProperty("account_number", out var an) ? an.GetString() : null;
            DateTimeOffset? txDate = null;
            if (x.TryGetProperty("transaction_date", out var td) && DateTimeOffset.TryParse(td.GetString(), out var p)) txDate = p;
            list.Add(new SePayTransaction(id, amountIn, content, refNo, acc, txDate));
        }

        return (true, null, list, null);
    }
}
