using LedgerFlow.Core.Domain;

namespace LedgerFlow.Infrastructure.Erp;

/// <summary>Posts a reconciled invoice into the accounting system. Faked in local/dev; a real HTTP client in prod.</summary>
public interface IErpPostingClient
{
    Task<ErpPostingResult> PostAsync(Invoice invoice, CancellationToken cancellationToken = default);
}

public sealed record ErpPostingResult(bool Accepted, string ErpDocumentId);
