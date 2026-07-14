// Mirrors LedgerFlow.Api.AnalyticsSummary.

export interface ExceptionReasonCount {
  code: string
  count: number
}

export interface SupplierVolume {
  supplierId: string
  invoices: number
  totalAmount: number
}

export interface AnalyticsSummary {
  totalProcessed: number
  autoPosted: number
  awaitingReview: number
  rejected: number
  straightThroughRate: number
  topExceptionReasons: ExceptionReasonCount[]
  topSuppliers: SupplierVolume[]
}

const BASE = import.meta.env.VITE_API_BASE ?? ''

export async function fetchAnalytics(days = 30): Promise<AnalyticsSummary> {
  const response = await fetch(`${BASE}/api/analytics?days=${days}`)
  if (!response.ok) {
    throw new Error(`${response.status} ${response.statusText}`)
  }
  return (await response.json()) as AnalyticsSummary
}

// Shown when the API is unreachable so the page still demonstrates its shape.
export const sampleAnalytics: AnalyticsSummary = {
  totalProcessed: 3872,
  autoPosted: 3489,
  awaitingReview: 214,
  rejected: 169,
  straightThroughRate: 0.901,
  topExceptionReasons: [
    { code: 'PriceOverTolerance', count: 88 },
    { code: 'NoGoodsReceipt', count: 61 },
    { code: 'LowConfidence', count: 43 },
    { code: 'MissingPurchaseOrder', count: 29 },
    { code: 'DuplicateInvoice', count: 12 },
  ],
  topSuppliers: [
    { supplierId: 'Meridian Print Ltd', invoices: 412, totalAmount: 288450.4 },
    { supplierId: 'Quantex Logistics', invoices: 365, totalAmount: 512310.0 },
    { supplierId: 'Delta Materials', invoices: 298, totalAmount: 194020.75 },
    { supplierId: 'Orion Tooling', invoices: 244, totalAmount: 421765.1 },
    { supplierId: 'Helix Chemicals', invoices: 201, totalAmount: 337980.6 },
  ],
}
