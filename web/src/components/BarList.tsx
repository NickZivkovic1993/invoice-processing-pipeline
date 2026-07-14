import { useState } from 'react'

export interface BarListItem {
  label: string
  value: number
}

/**
 * A single-measure horizontal bar list. One hue (color carries no identity here — length does),
 * bars anchored to the left baseline with a rounded data-end, 2px surface gaps, a direct value
 * label at each bar end, and a per-mark hover tooltip showing the share of the total.
 */
export function BarList({ items, accent = 'var(--accent)' }: { items: BarListItem[]; accent?: string }) {
  const [hovered, setHovered] = useState<number | null>(null)
  const max = Math.max(...items.map((i) => i.value), 1)
  const total = items.reduce((sum, i) => sum + i.value, 0)

  return (
    <div role="img" aria-label={`Bar chart: ${items.map((i) => `${i.label} ${i.value}`).join(', ')}`}>
      {items.map((item, index) => {
        const pct = (item.value / max) * 100
        const isHovered = hovered === index
        return (
          <div
            key={item.label}
            onMouseEnter={() => setHovered(index)}
            onMouseLeave={() => setHovered(null)}
            style={{
              display: 'grid',
              gridTemplateColumns: '160px 1fr 48px',
              alignItems: 'center',
              gap: 10,
              padding: '5px 0', // > mark height: the row is the hit target, not the bar
              position: 'relative',
              cursor: 'default',
            }}
          >
            <span
              style={{
                fontSize: 12,
                color: 'var(--text-dim)',
                overflow: 'hidden',
                textOverflow: 'ellipsis',
                whiteSpace: 'nowrap',
              }}
            >
              {item.label}
            </span>
            <span style={{ background: 'var(--panel-2)', borderRadius: 4, height: 14 }}>
              <span
                style={{
                  display: 'block',
                  width: `${pct}%`,
                  height: '100%',
                  background: accent,
                  borderRadius: '0 4px 4px 0', // rounded data-end; flat at the baseline
                  opacity: hovered === null || isHovered ? 1 : 0.45,
                  transition: 'opacity 120ms ease, width 400ms ease',
                }}
              />
            </span>
            <span className="mono" style={{ fontSize: 12, color: 'var(--text)', textAlign: 'right' }}>
              {item.value.toLocaleString('en-US')}
            </span>
            {isHovered && (
              <span
                role="tooltip"
                style={{
                  position: 'absolute',
                  left: 170,
                  top: '100%',
                  zIndex: 10,
                  background: 'var(--bg-elev)',
                  border: '1px solid var(--border)',
                  borderRadius: 8,
                  padding: '6px 10px',
                  fontSize: 12,
                  color: 'var(--text)',
                  whiteSpace: 'nowrap',
                  boxShadow: '0 4px 16px rgba(0,0,0,0.4)',
                }}
              >
                {item.label}: <b>{item.value.toLocaleString('en-US')}</b>
                <span style={{ color: 'var(--text-faint)' }}> · {((item.value / total) * 100).toFixed(1)}% of exceptions</span>
              </span>
            )}
          </div>
        )
      })}
    </div>
  )
}
