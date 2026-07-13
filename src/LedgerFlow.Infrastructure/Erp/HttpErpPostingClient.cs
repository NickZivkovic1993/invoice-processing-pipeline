using System.Net.Http.Json;
using LedgerFlow.Core.Domain;
using Microsoft.Extensions.Logging;

namespace LedgerFlow.Infrastructure.Erp;

/// <summary>Posts invoices to the ERP's REST endpoint. The payload is deliberately flat and boring.</summary>
public sealed class HttpErpPostingClient : IErpPostingClient
{
    private readonly HttpClient _http;
    private readonly ILogger<HttpErpPostingClient> _logger;

    public HttpErpPostingClient(HttpClient http, ILogger<HttpErpPostingClient> logger)
    {
        _http = http;
        _logger = logger;
    }

    public async Task<ErpPostingResult> PostAsync(Invoice invoice, CancellationToken cancellationToken = default)
    {
        var payload = new
        {
            invoice.InvoiceNumber,
            invoice.SupplierId,
            invoice.PurchaseOrderNumber,
            Amount = invoice.Total.Amount,
            invoice.Total.Currency,
            Date = invoice.InvoiceDate.ToString("yyyy-MM-dd"),
        };

        using var response = await _http.PostAsJsonAsync("api/payables", payload, cancellationToken).ConfigureAwait(false);
        response.EnsureSuccessStatusCode();

        var body = await response.Content.ReadFromJsonAsync<ErpResponse>(cancellationToken: cancellationToken)
            .ConfigureAwait(false);

        _logger.LogInformation("Posted invoice {InvoiceNumber} to ERP as {ErpId}.", invoice.InvoiceNumber, body?.Id);
        return new ErpPostingResult(true, body?.Id ?? string.Empty);
    }

    private sealed record ErpResponse(string Id);
}
