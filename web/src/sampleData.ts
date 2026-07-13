import type { ExceptionQueueItem } from './api/types'

// Shown only when the API is unreachable (e.g. the static site is opened without a running backend),
// so the interface still demonstrates its shape. Clearly flagged in the UI as illustrative.
export const sampleQueue: ExceptionQueueItem[] = [
  {
    id: '11111111-1111-1111-1111-111111111111',
    invoiceNumber: 'INV-88212',
    supplierId: 'Meridian Print Ltd',
    currency: 'EUR',
    totalAmount: 2140.55,
    extractionConfidence: 0.74,
    status: 'NeedsReview',
    receivedAt: '2026-07-12T09:07:00Z',
    exceptions: [
      { code: 'PriceOverTolerance', message: 'Invoiced total is 4.4% above the PO amount.', sku: null },
    ],
  },
  {
    id: '22222222-2222-2222-2222-222222222222',
    invoiceNumber: 'INV-88208',
    supplierId: 'Quantex Logistics',
    currency: 'EUR',
    totalAmount: 5320.0,
    extractionConfidence: 0.61,
    status: 'NeedsReview',
    exceptions: [{ code: 'MissingPurchaseOrder', message: 'No open PO PO-40877 was found.', sku: null }],
    receivedAt: '2026-07-12T08:51:00Z',
  },
  {
    id: '33333333-3333-3333-3333-333333333333',
    invoiceNumber: 'INV-88197',
    supplierId: 'Delta Materials',
    currency: 'EUR',
    totalAmount: 940.0,
    extractionConfidence: 0.82,
    status: 'NeedsReview',
    exceptions: [{ code: 'DuplicateInvoice', message: 'A matching invoice may already be posted.', sku: null }],
    receivedAt: '2026-07-12T08:32:00Z',
  },
]
