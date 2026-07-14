import { useEffect, useState } from 'react'
import { api } from './api/client'
import type { ExceptionQueueItem } from './api/types'
import { sampleQueue } from './sampleData'
import { Sidebar, type Page } from './components/Sidebar'
import { QueuePage } from './pages/QueuePage'
import { AnalyticsPage } from './pages/AnalyticsPage'

function pageFromHash(): Page {
  return window.location.hash === '#analytics' ? 'analytics' : 'queue'
}

export function App() {
  const [page, setPage] = useState<Page>(pageFromHash)
  const [items, setItems] = useState<ExceptionQueueItem[]>([])
  const [offline, setOffline] = useState(false)

  useEffect(() => {
    const onHashChange = () => setPage(pageFromHash())
    window.addEventListener('hashchange', onHashChange)
    return () => window.removeEventListener('hashchange', onHashChange)
  }, [])

  useEffect(() => {
    let cancelled = false
    api
      .listExceptions()
      .then((data) => {
        if (!cancelled) setItems(data)
      })
      .catch(() => {
        // No backend reachable — fall back to the illustrative sample so the UI still renders.
        if (!cancelled) {
          setOffline(true)
          setItems(sampleQueue)
        }
      })
    return () => {
      cancelled = true
    }
  }, [])

  return (
    <div className="app">
      <Sidebar queueCount={items.length} page={page} onNavigate={setPage} />
      <div className="main">
        <header className="header">
          <span className="page-title">{page === 'queue' ? 'Review Queue' : 'Analytics'}</span>
          <span className="crumb">/ {page === 'queue' ? 'Exceptions' : 'Throughput'}</span>
          <div className="avatar" style={{ marginLeft: 'auto' }}>
            NZ
          </div>
        </header>

        <main className="content">
          {page === 'queue' ? (
            <QueuePage
              items={items}
              offline={offline}
              onResolved={(id) => setItems((prev) => prev.filter((i) => i.id !== id))}
            />
          ) : (
            <AnalyticsPage />
          )}
          <p className="dl-note">LedgerFlow · reference implementation · figures are illustrative</p>
        </main>
      </div>
    </div>
  )
}
