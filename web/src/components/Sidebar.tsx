export function Sidebar({ queueCount }: { queueCount: number }) {
  return (
    <aside className="sidebar">
      <div className="brand">
        <div className="logo">
          <svg viewBox="0 0 24 24" fill="none" stroke="#fff" strokeWidth="2" strokeLinecap="round" strokeLinejoin="round">
            <path d="M7 3h7l4 4v14H7z" />
            <path d="M10 12h6M10 16h6M10 8h3" />
          </svg>
        </div>
        <div>
          <div className="name">LedgerFlow</div>
          <div className="sub">Invoice Pipeline</div>
        </div>
      </div>
      <div className="nav-label">Workspace</div>
      <a className="nav-item active" href="#queue">
        <svg viewBox="0 0 24 24" fill="none" stroke="currentColor" strokeWidth="2">
          <path d="M3 6h18M3 12h18M3 18h18" />
        </svg>
        Review Queue {queueCount > 0 && <span className="badge">{queueCount}</span>}
      </a>
      <div className="spacer" />
      <div className="side-card">
        <b>Tolerance policy</b>
        <br />
        Auto-post ≤ 2% PO variance
        <br />
        <span className="badge-s info" style={{ marginTop: 8 }}>
          <span className="d" /> 3-way match on
        </span>
      </div>
    </aside>
  )
}
