import { useEffect, useMemo, useState } from 'react'
import { useNavigate } from 'react-router-dom'
import { listTickets, isUsingFallback } from '../api/ticketsApi'
import { STATUSES, type Ticket } from '../types'
import StatusBadge from '../components/StatusBadge'
import NewTicketModal from '../components/NewTicketModal'
import { IssueOpenIcon } from '../components/icons'

function shortId(id: string): string {
  return '#' + id.slice(0, 7)
}

function formatDate(iso: string): string {
  return new Date(iso).toLocaleDateString(undefined, {
    month: 'short',
    day: 'numeric',
    year: 'numeric',
  })
}

type StatusFilter = 'All' | Ticket['status']

export default function TicketsPage() {
  const [tickets, setTickets] = useState<Ticket[]>([])
  const [loading, setLoading] = useState(true)
  const [statusFilter, setStatusFilter] = useState<StatusFilter>('All')
  const [query, setQuery] = useState('')
  const [showModal, setShowModal] = useState(false)
  const navigate = useNavigate()

  useEffect(() => {
    let alive = true
    listTickets().then((data) => {
      if (!alive) return
      setTickets(data)
      setLoading(false)
    })
    return () => { alive = false }
  }, [])

  const filtered = useMemo(() => {
    const q = query.trim().toLowerCase()
    return tickets.filter((t) => {
      if (statusFilter !== 'All' && t.status !== statusFilter) return false
      if (!q) return true
      return (
        t.name.toLowerCase().includes(q) ||
        t.description.toLowerCase().includes(q)
      )
    })
  }, [tickets, statusFilter, query])

  const openCount = filtered.filter(
    (t) => t.status === 'New' || t.status === 'In Progress'
  ).length

  return (
    <div className="container">
      <div className="page-head">
        <h1>Support Tickets</h1>
        <span className="spacer" />
        <button className="btn btn-primary" onClick={() => setShowModal(true)}>
          New ticket
        </button>
      </div>

      {isUsingFallback() && (
        <div className="notice">
          Backend not reachable — showing in-memory demo data. Changes won’t persist.
        </div>
      )}

      <div className="toolbar">
        <select className="filter" value={statusFilter}
          onChange={(e) => setStatusFilter(e.target.value as StatusFilter)}>
          <option value="All">All statuses</option>
          {STATUSES.map((s) => <option key={s} value={s}>{s}</option>)}
        </select>
        <input className="search" placeholder="Search by name or description…"
          value={query} onChange={(e) => setQuery(e.target.value)} />
      </div>

      <div className="list">
        <div className="list-head">
          <span className="open">
            <IssueOpenIcon size={16} /> {openCount} Open
          </span>
          <span className="count">{filtered.length} total</span>
        </div>

        {loading ? (
          <div className="empty">Loading tickets…</div>
        ) : filtered.length === 0 ? (
          <div className="empty">No tickets match your filters.</div>
        ) : (
          filtered.map((t) => (
            <div key={t.id} className="row" onClick={() => navigate(`/tickets/${t.id}`)}>
              <div className="main">
                <div className="title">{t.description}</div>
                <div className="meta">
                  <span className="mono">{shortId(t.id)}</span> opened {formatDate(t.createdAt)} by {t.name}
                </div>
                {t.summary && <div className="summary">🤖 {t.summary}</div>}
              </div>
              <div className="right">
                <StatusBadge status={t.status} />
              </div>
            </div>
          ))
        )}
      </div>

      {showModal && (
        <NewTicketModal
          onClose={() => setShowModal(false)}
          onCreated={(ticket) => {
            setShowModal(false)
            setTickets((prev) => [ticket, ...prev])
            navigate(`/tickets/${ticket.id}`)
          }}
        />
      )}
    </div>
  )
}
