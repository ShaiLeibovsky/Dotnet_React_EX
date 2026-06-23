import { useEffect, useState } from 'react'
import { useParams, Link } from 'react-router-dom'
import { getTicket, updateTicket } from '../api/ticketsApi'
import { STATUSES, type Ticket, type TicketStatus } from '../types'
import StatusBadge from '../components/StatusBadge'
import { SparkleIcon } from '../components/icons'
import { useAuth } from '../auth'

export default function TicketDetailPage() {
  const { id = '' } = useParams<{ id: string }>()
  const { isAdmin } = useAuth()
  const [ticket, setTicket] = useState<Ticket | null>(null)
  const [loading, setLoading] = useState(true)
  const [status, setStatus] = useState<TicketStatus>('New')
  const [resolution, setResolution] = useState('')
  const [saving, setSaving] = useState(false)
  const [toast, setToast] = useState('')

  useEffect(() => {
    let alive = true
    getTicket(id).then((t) => {
      if (!alive) return
      setTicket(t)
      if (t) {
        setStatus(t.status)
        setResolution(t.resolution || '')
      }
      setLoading(false)
    })
    return () => { alive = false }
  }, [id])

  const dirty =
    ticket !== null &&
    (status !== ticket.status || resolution !== (ticket.resolution || ''))

  async function save() {
    if (!ticket) return
    setSaving(true)
    try {
      const updated = await updateTicket(id, { status, resolution })
      const notes: string[] = []
      if (updated.status !== ticket.status) notes.push('status change')
      if ((updated.resolution || '') !== (ticket.resolution || '')) notes.push('resolution update')
      setTicket(updated)
      setToast(`Saved. Email sent to customer (${notes.join(' + ') || 'no change'}).`)
      setTimeout(() => setToast(''), 3500)
    } finally {
      setSaving(false)
    }
  }

  if (loading) return <div className="container"><p>Loading…</p></div>
  if (!ticket) {
    return (
      <div className="container">
        <p>Ticket not found. <Link to="/">Back to all tickets</Link></p>
      </div>
    )
  }

  return (
    <div className="container">
      <div className="detail-head">
        <h1>
          {ticket.description} <span className="num mono">#{ticket.id.slice(0, 7)}</span>
        </h1>
        <div className="detail-sub">
          <StatusBadge status={ticket.status} />
          <span><strong>{ticket.name}</strong> opened this ticket</span>
          <span>· {new Date(ticket.createdAt).toLocaleString()}</span>
        </div>
      </div>

      <div className="detail-grid">
        <div>
          <div className="card">
            <div className="comment-head">
              <strong>{ticket.name}</strong> &lt;{ticket.email}&gt; described the issue
            </div>
            <div className="comment-body">
              <p style={{ marginTop: 0 }}>{ticket.description}</p>
              {ticket.summary && (
                <div className="ai-box">
                  <div className="tag"><SparkleIcon /> AI summary</div>
                  {ticket.summary}
                </div>
              )}
            </div>
          </div>

          <div className="card" style={{ marginTop: 16 }}>
            <div className="comment-head"><strong>Resolution</strong></div>
            <div className="comment-body">
              {isAdmin ? (
                <textarea className="textarea" value={resolution}
                  placeholder="Add or edit the resolution notes…"
                  onChange={(e) => setResolution(e.target.value)} />
              ) : (
                <p style={{ margin: 0, color: ticket.resolution ? 'inherit' : 'var(--fg-muted)' }}>
                  {ticket.resolution || 'No resolution yet.'}
                </p>
              )}
            </div>
          </div>
        </div>

        <div className="sidebar">
          <div className="block">
            <h3>STATUS</h3>
            {isAdmin ? (
              <select className="select" value={status}
                onChange={(e) => setStatus(e.target.value as TicketStatus)}>
                {STATUSES.map((s) => <option key={s} value={s}>{s}</option>)}
              </select>
            ) : (
              <StatusBadge status={ticket.status} />
            )}
          </div>

          <div className="block">
            <h3>CUSTOMER</h3>
            <div className="kv"><span className="k">Name:</span> {ticket.name}</div>
            <div className="kv"><span className="k">Email:</span> {ticket.email}</div>
          </div>

          <div className="block">
            <h3>TICKET ID</h3>
            <div className="kv mono">{ticket.id}</div>
          </div>

          <div style={{ paddingBottom: 16 }}>
            <h3>UPDATED</h3>
            <div className="kv">{new Date(ticket.updatedAt).toLocaleString()}</div>
          </div>

          {isAdmin ? (
            <button className="btn btn-primary" style={{ width: '100%' }}
              onClick={save} disabled={!dirty || saving}>
              {saving ? 'Saving…' : 'Save changes'}
            </button>
          ) : (
            <div className="kv" style={{ color: 'var(--fg-muted)' }}>
              <Link to="/login">Sign in</Link> as admin to edit.
            </div>
          )}
          <Link to="/" style={{ display: 'inline-block', marginTop: 12, fontSize: 13 }}>
            ← All tickets
          </Link>
        </div>
      </div>

      {toast && <div className="toast">{toast}</div>}
    </div>
  )
}
