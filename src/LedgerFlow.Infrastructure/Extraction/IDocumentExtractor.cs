using LedgerFlow.Core.Domain;

namespace LedgerFlow.Infrastructure.Extraction;

/// <summary>
/// Turns a raw invoice document (PDF/image bytes) into a structured <see cref="Invoice"/>.
/// The port exists so the pipeline and its tests never depend on Azure directly; the production
/// implementation is <see cref="AzureDocumentIntelligenceExtractor"/>.
/// </summary>
public interface IDocumentExtractor
{
    Task<Invoice> ExtractAsync(Stream document, string contentType, CancellationToken cancellationToken = default);
}
