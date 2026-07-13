// Confidence colour follows the same thresholds the matcher uses: below 80% is where a human looks.
export function ConfidenceBar({ value }: { value: number }) {
  const pct = Math.round(value * 100)
  const colour = value >= 0.85 ? '#34d399' : value >= 0.8 ? '#fbbf24' : '#f87171'
  return (
    <span className="conf">
      <span className="bar">
        <span style={{ width: `${pct}%`, background: colour }} />
      </span>
      <span className="num">{pct}%</span>
    </span>
  )
}
