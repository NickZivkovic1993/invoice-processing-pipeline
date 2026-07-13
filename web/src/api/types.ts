// Mirrors LedgerFlow.Api.Contracts. Kept hand-written and small; the API is the source of truth.

export interface ExceptionDetail {
  code: string
  message: string
  sku: string | null
}

export interface ExceptionQueueItem {
  id: string
  invoiceNumber: string
  supplierId: string
  currency: string
  totalAmount: number
  extractionConfidence: number
  status: string
  receivedAt: string
  exceptions: ExceptionDetail[]
}

export type InvoiceStatus = 'NeedsReview' | 'Posted' | 'Rejected'
