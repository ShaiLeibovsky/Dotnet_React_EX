import { useEffect, useState } from 'react'
import { Link, useParams } from 'react-router-dom'
import { toast } from 'sonner'
import { ArrowLeft, Sparkles } from 'lucide-react'
import { Button } from '@/components/ui/button'
import { Card, CardContent, CardHeader, CardTitle } from '@/components/ui/card'
import { Textarea } from '@/components/ui/textarea'
import {
  Select,
  SelectContent,
  SelectItem,
  SelectTrigger,
  SelectValue,
} from '@/components/ui/select'
import { StatusBadge } from '@/components/tickets/StatusBadge'
import { ResponseThread } from '@/components/tickets/ResponseThread'
import { getTicket, updateTicket, addResponse } from '@/api/ticketsApi'
import { formatDateTime } from '@/lib/format'
import { useAuth } from '@/context/AuthContext'
import { STATUSES, type Ticket, type TicketStatus } from '@/types/ticket'

export function TicketDetailPage() {
  const { id = '' } = useParams<{ id: string }>()
  const { user, isAdmin } = useAuth()
  const [ticket, setTicket] = useState<Ticket | null>(null)
  const [loading, setLoading] = useState(true)
  const [status, setStatus] = useState<TicketStatus>('New')
  const [resolution, setResolution] = useState('')
  const [saving, setSaving] = useState(false)

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
    return () => {
      alive = false
    }
  }, [id])

  const dirty =
    ticket !== null &&
    (status !== ticket.status || resolution !== (ticket.resolution || ''))

  async function save() {
    if (!ticket) return
    setSaving(true)
    try {
      const prev = ticket
      const updated = await updateTicket(id, { status, resolution })
      setTicket(updated)
      const notes: string[] = []
      if (updated.status !== prev.status) notes.push('status change')
      if ((updated.resolution || '') !== (prev.resolution || ''))
        notes.push('resolution update')
      toast.success('Changes saved', {
        description: `Simulated email sent to customer (${notes.join(' + ') || 'no change'}).`,
      })
    } finally {
      setSaving(false)
    }
  }

  async function respond(body: string) {
    const updated = await addResponse(id, {
      author: user?.email ?? 'Support Admin',
      role: 'admin',
      body,
    })
    setTicket(updated)
    toast.success('Response posted', {
      description: 'Simulated email sent to customer.',
    })
  }

  if (loading) {
    return <div className="mx-auto max-w-5xl px-4 py-8">Loading…</div>
  }
  if (!ticket) {
    return (
      <div className="mx-auto max-w-5xl px-4 py-8">
        Ticket not found.{' '}
        <Link className="underline" to="/">
          Back to all tickets
        </Link>
      </div>
    )
  }

  return (
    <div className="mx-auto max-w-5xl px-4 py-8">
      <div className="mb-2">
        <h1 className="text-2xl font-normal">
          {ticket.description}{' '}
          <span className="text-muted-foreground font-mono">
            #{ticket.id.slice(0, 7)}
          </span>
        </h1>
      </div>
      <div className="text-muted-foreground mb-6 flex flex-wrap items-center gap-2 border-b pb-4 text-sm">
        <StatusBadge status={ticket.status} />
        <span>
          <strong className="text-foreground">{ticket.name}</strong> opened this ticket
        </span>
        <span>· {formatDateTime(ticket.createdAt)}</span>
      </div>

      <div className="grid gap-6 md:grid-cols-[1fr_280px]">
        <div className="space-y-6">
          <Card>
            <CardHeader>
              <CardTitle className="text-sm font-normal">
                <strong>{ticket.name}</strong>{' '}
                <span className="text-muted-foreground">
                  &lt;{ticket.email}&gt; described the issue
                </span>
              </CardTitle>
            </CardHeader>
            <CardContent className="space-y-3">
              <p className="text-sm whitespace-pre-wrap">{ticket.description}</p>
              {ticket.summary && (
                <div className="rounded-md border border-sky-200 bg-sky-50 p-3 text-sm text-sky-900">
                  <div className="mb-1 flex items-center gap-1 font-medium">
                    <Sparkles className="size-3.5" /> AI summary
                  </div>
                  {ticket.summary}
                </div>
              )}
            </CardContent>
          </Card>

          <Card>
            <CardHeader>
              <CardTitle className="text-base">Responses</CardTitle>
            </CardHeader>
            <CardContent>
              <ResponseThread
                responses={ticket.responses}
                canRespond={isAdmin}
                onRespond={respond}
              />
            </CardContent>
          </Card>
        </div>

        <aside className="space-y-6 text-sm">
          <div>
            <h3 className="text-muted-foreground mb-2 text-xs font-semibold">STATUS</h3>
            {isAdmin ? (
              <Select value={status} onValueChange={(v) => setStatus(v as TicketStatus)}>
                <SelectTrigger className="w-full">
                  <SelectValue />
                </SelectTrigger>
                <SelectContent>
                  {STATUSES.map((s) => (
                    <SelectItem key={s} value={s}>
                      {s}
                    </SelectItem>
                  ))}
                </SelectContent>
              </Select>
            ) : (
              <StatusBadge status={ticket.status} />
            )}
          </div>

          <div className="border-t pt-4">
            <h3 className="text-muted-foreground mb-2 text-xs font-semibold">
              RESOLUTION
            </h3>
            {isAdmin ? (
              <Textarea
                value={resolution}
                onChange={(e) => setResolution(e.target.value)}
                placeholder="Add or edit resolution notes…"
                rows={4}
              />
            ) : (
              <p className={ticket.resolution ? '' : 'text-muted-foreground'}>
                {ticket.resolution || 'No resolution yet.'}
              </p>
            )}
          </div>

          <div className="border-t pt-4">
            <h3 className="text-muted-foreground mb-2 text-xs font-semibold">CUSTOMER</h3>
            <p>{ticket.name}</p>
            <p className="text-muted-foreground">{ticket.email}</p>
          </div>

          <div className="border-t pt-4">
            <h3 className="text-muted-foreground mb-2 text-xs font-semibold">
              TICKET ID
            </h3>
            <p className="font-mono text-xs break-all">{ticket.id}</p>
            <p className="text-muted-foreground mt-2 text-xs">
              Updated {formatDateTime(ticket.updatedAt)}
            </p>
          </div>

          {isAdmin ? (
            <Button className="w-full" onClick={save} disabled={!dirty || saving}>
              {saving ? 'Saving…' : 'Save changes'}
            </Button>
          ) : (
            <p className="text-muted-foreground">
              <Link className="underline" to="/login">
                Sign in
              </Link>{' '}
              as admin to respond and edit.
            </p>
          )}

          <Button asChild variant="link" className="px-0">
            <Link to="/">
              <ArrowLeft className="size-4" /> All tickets
            </Link>
          </Button>
        </aside>
      </div>
    </div>
  )
}
