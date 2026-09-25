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
import { ErrorRetry } from '@/components/tickets/ErrorRetry'
import { ApiError, getTicket, updateTicket } from '@/api/ticketsApi'
import { formatDateTime } from '@/lib/format'
import { useAuth } from '@/context/AuthContext'
import { STATUSES, type Ticket, type TicketStatus } from '@/types/ticket'

export const TicketDetailPage = () => {
    const { id = '' } = useParams<{ id: string }>()
    const { isAdmin } = useAuth()
    const [ticket, setTicket] = useState<Ticket | null>(null)
    const [loading, setLoading] = useState(true)
    const [loadError, setLoadError] = useState<string | null>(null)
    const [reloadCount, setReloadCount] = useState(0)
    const [status, setStatus] = useState<TicketStatus>('New')
    const [resolution, setResolution] = useState('')
    const [saving, setSaving] = useState(false)

    useEffect(() => {
        let alive = true
        setLoading(true)
        setLoadError(null)
        getTicket(id)
            .then((loaded) => {
                if (!alive) return
                setTicket(loaded)
                setStatus(loaded.status)
                setResolution(loaded.resolution || '')
                setLoading(false)
            })
            .catch((error: unknown) => {
                if (!alive) return
                setLoadError(
                    error instanceof ApiError && error.status === 404
                        ? 'No ticket exists with this id.'
                        : 'Could not load this ticket. The server is unreachable.',
                )
                setLoading(false)
            })
        return () => {
            alive = false
        }
    }, [id, reloadCount])

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
        } catch {
            toast.error('Could not save changes', {
                description: 'The server is unreachable. Try again.',
            })
        } finally {
            setSaving(false)
        }
    }

    if (loading) {
        return <div className="mx-auto max-w-5xl px-4 py-8">Loading…</div>
    }
    if (loadError || !ticket) {
        return (
            <div className="mx-auto max-w-5xl px-4 py-8">
                <ErrorRetry
                    message={loadError ?? 'Could not load this ticket.'}
                    onRetry={() => setReloadCount((count) => count + 1)}
                />
                <Button asChild variant="link" className="mt-4 px-0">
                    <Link to="/">
                        <ArrowLeft className="size-4" /> All tickets
                    </Link>
                </Button>
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
                    <strong className="text-foreground">{ticket.name}</strong> opened this
                    ticket
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
                            <p className="text-sm whitespace-pre-wrap">
                                {ticket.description}
                            </p>
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
                </div>

                <aside className="space-y-6 text-sm">
                    <div>
                        <h3 className="text-muted-foreground mb-2 text-xs font-semibold">
                            STATUS
                        </h3>
                        {isAdmin ? (
                            <Select
                                value={status}
                                onValueChange={(v) => setStatus(v as TicketStatus)}
                            >
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
                            <p
                                className={
                                    ticket.resolution ? '' : 'text-muted-foreground'
                                }
                            >
                                {ticket.resolution || 'No resolution yet.'}
                            </p>
                        )}
                    </div>

                    <div className="border-t pt-4">
                        <h3 className="text-muted-foreground mb-2 text-xs font-semibold">
                            CUSTOMER
                        </h3>
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
                        <Button
                            className="w-full"
                            onClick={save}
                            disabled={!dirty || saving}
                        >
                            {saving ? 'Saving…' : 'Save changes'}
                        </Button>
                    ) : (
                        <p className="text-muted-foreground">
                            <Link className="underline" to="/login">
                                Sign in
                            </Link>{' '}
                            as admin to edit.
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
