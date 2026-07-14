import { useEffect, useState } from 'react'
import { fetchAnalytics, sampleAnalytics, type AnalyticsSummary } from '../api/analytics'
import { BarList } from '../components/BarList'

const REASON_LABELS: Record<string, string> = {
  PriceOverTolerance: 'Price over tolerance',
  NoGoodsReceipt: 'No goods receipt',
  LowConfidence: 'Low extraction confidence',
  MissingPurchaseOrder: 'No PO match',
  DuplicateInvoice: 'Possible duplicate',
  SupplierMismatch: 'Supplier mismatch',
  CurrencyMismatch: 'Currency mismatch',
  UnorderedItem: 'Unordered item',
  QuantityOverReceipt: 'Quantity over receipt',
  TotalsMismatch: 'Totals mismatch',
}

function formatMoney(amount: number): string {
  return new Intl.NumberFormat('en-GB', { style: 'currency', currency: 'EUR', maximumFractionDigits: 0 }).format(amount)
}

function StatTile({ label, value, sub, tone }: { label: string; value: string; sub?: string; tone?: string }) {
  return (
    <div className="card kpi rise">
      <div className="top">{label}</div>
      <div className="val" style={{ fontSize: 26, color: tone }}>{value}</div>
      {sub && (
        <div className="delta">
          <span className="muted">{sub}</span>
        </div>
      )}
    </div>
  )
}

export function AnalyticsPage() {
  const [data, setData] = useState<AnalyticsSummary | null>(null)
  const [offline, setOffline] = useState(false)
  const [days, setDays] = useState(30)

  useEffect(() => {
    let cancelled = false
    fetchAnalytics(days)
      .then((d) => {
        if (!cancelled) {
          setData(d)
          setOffline(false)
        }
      })
      .catch(() => {
        if (!cancelled) {
          setData(sampleAnalytics)
          setOffline(true)
        }
      })
    return () => {
      cancelled = true
    }
  }, [days])

  if (!data) {
    return <p className="dl-note">Loading…</p>
  }

  const reasons = data.topExceptionReasons.map((r) => ({
    label: REASON_LABELS[r.code] ?? r.code,
    value: r.count,
  }))

  return (
    <>
      <div className="page-head">
        <div className="rise">
          <h1>Analytics</h1>
          <p>Throughput and exception patterns — what flows straight through, and what stalls where.</p>
        </div>
        <div className="head-actions rise" data-tabs>
          {[7, 30, 90].map((d) => (
            <button key={d} className={`btn${days === d ? ' primary' : ''}`} onClick={() => setDays(d)}>
              {d}d
            </button>
          ))}
        </div>
      </div>

      {offline && (
        <p className="dl-note" style={{ marginBottom: 14 }}>
          API not reachable — showing illustrative sample data.
        </p>
      )}

      <div className="grid cols-4" style={{ marginBottom: 18 }}>
        <StatTile
          label="Straight-through rate"
          value={`${(data.straightThroughRate * 100).toFixed(1)}%`}
          sub="posted with no human touch"
          tone="var(--good)"
        />
        <StatTile label="Invoices processed" value={data.totalProcessed.toLocaleString('en-US')} sub={`last ${days} days`} />
        <StatTile label="Awaiting review" value={data.awaitingReview.toLocaleString('en-US')} tone="var(--warn)" />
        <StatTile label="Rejected" value={data.rejected.toLocaleString('en-US')} tone="var(--bad)" />
      </div>

      <div className="grid" style={{ gridTemplateColumns: '1.2fr 1fr', alignItems: 'start' }}>
        <div className="card rise">
          <div className="card-head">
            <div>
              <h3>Why invoices stall</h3>
              <span className="sub">Exception reasons, last {days} days</span>
            </div>
          </div>
          {reasons.length > 0 ? (
            <BarList items={reasons} />
          ) : (
            <p className="dl-note">No exceptions in this window.</p>
          )}
        </div>

        <div className="card rise-2">
          <div className="card-head">
            <div>
              <h3>Top suppliers</h3>
              <span className="sub">By invoice count</span>
            </div>
          </div>
          <div className="table-wrap">
            <table className="data">
              <thead>
                <tr>
                  <th>Supplier</th>
                  <th className="right">Invoices</th>
                  <th className="right">Amount</th>
                </tr>
              </thead>
              <tbody>
                {data.topSuppliers.map((s) => (
                  <tr key={s.supplierId}>
                    <td>{s.supplierId}</td>
                    <td className="right mono">{s.invoices.toLocaleString('en-US')}</td>
                    <td className="right amt">{formatMoney(s.totalAmount)}</td>
                  </tr>
                ))}
              </tbody>
            </table>
          </div>
        </div>
      </div>
    </>
  )
}
