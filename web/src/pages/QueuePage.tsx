import { useMemo, useState } from 'react'
import { api } from '../api/client'
import type { ExceptionQueueItem } from '../api/types'
import { ConfidenceBar } from '../components/ConfidenceBar'
import { ExceptionBadge } from '../components/ExceptionBadge'

function formatMoney(amount: number, currency: string): string {
  return new Intl.NumberFormat('en-GB', { style: 'currency', currency }).format(amount)
}

export function QueuePage({
  items,
  offline,
  onResolved,
}: {
  items: ExceptionQueueItem[]
  offline: boolean
  onResolved: (id: string) => void
}) {
  const [selectedId, setSelectedId] = useState<string | null>(items[0]?.id ?? null)
  const [busy, setBusy] = useState(false)

  const selected = useMemo(
    () => items.find((i) => i.id === selectedId) ?? items[0] ?? null,
    [items, selectedId],
  )

  async function resolve(id: string, action: 'approve' | 'reject') {
    if (offline) {
      onResolved(id)
      return
    }
    setBusy(true)
    try {
      await (action === 'approve' ? api.approve(id) : api.reject(id))
      onResolved(id)
    } finally {
      setBusy(false)
    }
  }

  return (
    <>
      <div className="page-head">
        <div className="rise">
          <h1>Review Queue</h1>
          <p>
            Documents held for a human decision — low confidence, a failed match, or a policy flag.
            Everything else posts automatically.
          </p>
        </div>
      </div>

      {offline && (
        <p className="dl-note" style={{ marginBottom: 14 }}>
          API not reachable — showing illustrative sample data.
        </p>
      )}

      <div className="grid" style={{ gridTemplateColumns: '1.35fr 1fr', alignItems: 'start' }}>
        <div className="card rise">
          <div className="card-head">
            <div>
              <h3>Held documents</h3>
              <span className="sub">{items.length} awaiting review</span>
            </div>
          </div>
          <div className="table-wrap">
            <table className="data">
              <thead>
                <tr>
                  <th>Invoice</th>
                  <th className="right">Amount</th>
                  <th>Flag</th>
                  <th>Conf.</th>
                </tr>
              </thead>
              <tbody>
                {items.map((item) => (
                  <tr
                    key={item.id}
                    onClick={() => setSelectedId(item.id)}
                    style={{ cursor: 'pointer', background: item.id === selected?.id ? 'var(--panel)' : undefined }}
                  >
                    <td>
                      <span className="mono">{item.invoiceNumber}</span>
                      <div className="cell-sub">{item.supplierId}</div>
                    </td>
                    <td className="right amt">{formatMoney(item.totalAmount, item.currency)}</td>
                    <td>{item.exceptions[0] && <ExceptionBadge detail={item.exceptions[0]} />}</td>
                    <td>
                      <ConfidenceBar value={item.extractionConfidence} />
                    </td>
                  </tr>
                ))}
                {items.length === 0 && (
                  <tr>
                    <td colSpan={4} className="faint" style={{ padding: 20, textAlign: 'center' }}>
                      Queue is clear — every invoice posted automatically.
                    </td>
                  </tr>
                )}
              </tbody>
            </table>
          </div>
        </div>

        <div className="card rise-2">
          {selected ? (
            <>
              <div className="card-head">
                <div>
                  <h3>{selected.invoiceNumber}</h3>
                  <span className="sub">{selected.supplierId}</span>
                </div>
                {selected.exceptions[0] && <ExceptionBadge detail={selected.exceptions[0]} />}
              </div>

              <div style={{ fontSize: 11, color: 'var(--text-faint)', textTransform: 'uppercase', letterSpacing: '.06em', margin: '4px 0 10px' }}>
                Why it was held
              </div>
              <div className="feed">
                {selected.exceptions.map((ex, idx) => (
                  <div
                    key={idx}
                    className="flex between"
                    style={{ padding: '8px 0', borderBottom: '1px solid var(--border-soft)' }}
                  >
                    <span className="faint">{ex.sku ?? '—'}</span>
                    <span>{ex.message}</span>
                  </div>
                ))}
              </div>

              <div className="divider" />
              <div className="flex gap-8">
                <button
                  className="btn primary"
                  style={{ flex: 1, justifyContent: 'center' }}
                  disabled={busy}
                  onClick={() => resolve(selected.id, 'approve')}
                >
                  Approve &amp; post
                </button>
                <button
                  className="btn"
                  style={{ flex: 1, justifyContent: 'center' }}
                  disabled={busy}
                  onClick={() => resolve(selected.id, 'reject')}
                >
                  Reject
                </button>
              </div>
            </>
          ) : (
            <div className="faint" style={{ padding: 20 }}>
              Select an invoice to review.
            </div>
          )}
        </div>
      </div>
    </>
  )
}
