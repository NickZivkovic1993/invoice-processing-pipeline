import type { ExceptionDetail } from '../api/types'

// Map each match exception to a severity colour and a short human label.
const SEVERITY: Record<string, { tone: 'warn' | 'bad' | 'info'; label: string }> = {
  LowConfidence: { tone: 'warn', label: 'Low confidence' },
  DuplicateInvoice: { tone: 'info', label: 'Duplicate?' },
  MissingPurchaseOrder: { tone: 'bad', label: 'No PO match' },
  SupplierMismatch: { tone: 'bad', label: 'Supplier mismatch' },
  CurrencyMismatch: { tone: 'bad', label: 'Currency mismatch' },
  UnorderedItem: { tone: 'warn', label: 'Unordered item' },
  PriceOverTolerance: { tone: 'warn', label: 'Over tolerance' },
  QuantityOverReceipt: { tone: 'warn', label: 'Over receipt' },
  NoGoodsReceipt: { tone: 'bad', label: 'No receipt' },
  TotalsMismatch: { tone: 'warn', label: 'Totals mismatch' },
}

export function ExceptionBadge({ detail }: { detail: ExceptionDetail }) {
  const meta = SEVERITY[detail.code] ?? { tone: 'warn' as const, label: detail.code }
  return (
    <span className={`badge-s ${meta.tone}`} title={detail.message}>
      <span className="d" /> {meta.label}
    </span>
  )
}
