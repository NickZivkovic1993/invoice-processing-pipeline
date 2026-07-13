import type { ExceptionQueueItem, InvoiceStatus } from './types'

const BASE = import.meta.env.VITE_API_BASE ?? ''

async function json<T>(response: Response): Promise<T> {
  if (!response.ok) {
    throw new Error(`${response.status} ${response.statusText}`)
  }
  return response.json() as Promise<T>
}

export const api = {
  listExceptions(status: InvoiceStatus = 'NeedsReview'): Promise<ExceptionQueueItem[]> {
    return fetch(`${BASE}/api/exceptions?status=${status}`).then(json<ExceptionQueueItem[]>)
  },

  getException(id: string): Promise<ExceptionQueueItem> {
    return fetch(`${BASE}/api/exceptions/${id}`).then(json<ExceptionQueueItem>)
  },

  approve(id: string): Promise<{ id: string; status: string }> {
    return fetch(`${BASE}/api/exceptions/${id}/approve`, {
      method: 'POST',
      headers: { 'content-type': 'application/json' },
      body: JSON.stringify({ note: null }),
    }).then(json<{ id: string; status: string }>)
  },

  reject(id: string): Promise<{ id: string; status: string }> {
    return fetch(`${BASE}/api/exceptions/${id}/reject`, {
      method: 'POST',
      headers: { 'content-type': 'application/json' },
      body: JSON.stringify({ note: null }),
    }).then(json<{ id: string; status: string }>)
  },
}
