import { useState, type FormEvent } from 'react'
import { createTicket } from '../api/ticketsApi'
import type { Ticket } from '../types'

const emailRe = /^[^\s@]+@[^\s@]+\.[^\s@]+$/

interface Props {
  onClose: () => void
  onCreated: (ticket: Ticket) => void
}

interface Errors {
  name?: string
  email?: string
  description?: string
}

export default function NewTicketModal({ onClose, onCreated }: Props) {
  const [name, setName] = useState('')
  const [email, setEmail] = useState('')
  const [description, setDescription] = useState('')
  const [errors, setErrors] = useState<Errors>({})
  const [saving, setSaving] = useState(false)

  function validate(): boolean {
    const e: Errors = {}
    if (!name.trim()) e.name = 'Full name is required'
    if (!email.trim()) e.email = 'Email is required'
    else if (!emailRe.test(email)) e.email = 'Enter a valid email'
    if (!description.trim()) e.description = 'Issue description is required'
    setErrors(e)
    return Object.keys(e).length === 0
  }

  async function submit(ev: FormEvent) {
    ev.preventDefault()
    if (!validate()) return
    setSaving(true)
    try {
      const ticket = await createTicket({
        name: name.trim(),
        email: email.trim(),
        description: description.trim(),
      })
      onCreated(ticket)
    } finally {
      setSaving(false)
    }
  }

  return (
    <div className="overlay" onClick={onClose}>
      <form className="modal" onClick={(e) => e.stopPropagation()} onSubmit={submit}>
        <div className="modal-head">
          <h2>Open a new ticket</h2>
          <button type="button" className="x" onClick={onClose} aria-label="Close">×</button>
        </div>
        <div className="modal-body">
          <div className="field">
            <label htmlFor="nt-name">Full name</label>
            <input id="nt-name" className="input" value={name}
              onChange={(e) => setName(e.target.value)} placeholder="Jane Smith" />
            {errors.name && <div className="error">{errors.name}</div>}
          </div>
          <div className="field">
            <label htmlFor="nt-email">Email address</label>
            <input id="nt-email" className="input" value={email}
              onChange={(e) => setEmail(e.target.value)} placeholder="jane@example.com" />
            {errors.email && <div className="error">{errors.email}</div>}
          </div>
          <div className="field">
            <label htmlFor="nt-desc">Issue description</label>
            <textarea id="nt-desc" className="textarea" value={description}
              onChange={(e) => setDescription(e.target.value)}
              placeholder="Describe what went wrong…" />
            {errors.description && <div className="error">{errors.description}</div>}
          </div>
        </div>
        <div className="modal-foot">
          <button type="button" className="btn" onClick={onClose}>Cancel</button>
          <button type="submit" className="btn btn-primary" disabled={saving}>
            {saving ? 'Submitting…' : 'Submit new ticket'}
          </button>
        </div>
      </form>
    </div>
  )
}
