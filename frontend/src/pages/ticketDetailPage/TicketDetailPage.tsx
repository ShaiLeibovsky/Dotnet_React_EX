import { useState } from 'react'
import { Link, useLoaderData, useRevalidator } from 'react-router-dom'
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
import { updateTicket } from '@/api/ticketsApi'
import { useRevalidateUntilSummary } from '@/hooks/useRevalidateUntilSummary'
import { formatDateTime } from '@/lib/format'
import { useAuth } from '@/context/AuthContext'
import { STATUSES, type Ticket, type TicketStatus } from '@/types/ticket'

export const TicketDetailPage = () => {
    const ticket = useLoaderData() as Ticket
    const { isAdmin } = useAuth()
    const revalidator = useRevalidator()
    const [editedTicketId, setEditedTicketId] = useState(ticket.id)
    const [status, setStatus] = useState<TicketStatus>(ticket.status)
    const [resolution, setResolution] = useState(ticket.resolution)
    const [saving, setSaving] = useState(false)

    const summaryStillExpected = useRevalidateUntilSummary(ticket)

    if (editedTicketId !== ticket.id) {
        setEditedTicketId(ticket.id)
        setStatus(ticket.status)
        setResolution(ticket.resolution)
    }

    const dirty = status !== ticket.status || resolution !== ticket.resolution

    async function save() {
        setSaving(true)
        try {
            const updated = await updateTicket(ticket.id, { status, resolution })
            const notes: string[] = []
            if (updated.status !== ticket.status) notes.push('status change')
            if (updated.resolution !== ticket.resolution) notes.push('resolution update')
            revalidator.revalidate()
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

    return (
        <div className="mx-auto max-w-5xl px-4 py-8">
            <div className="mb-2">
                <h1 className="text-2xl font-normal break-words">
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
                            <CardTitle className="text-sm font-normal break-words">
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
                            {ticket.summary ? (
                                <div className="rounded-md border border-sky-200 bg-sky-50 p-3 text-sm text-sky-900">
                                    <div className="mb-1 flex items-center gap-1 font-medium">
                                        <Sparkles className="size-3.5" /> AI summary
                                    </div>
                                    {ticket.summary}
                                </div>
                            ) : (
                                summaryStillExpected && (
                                    <p className="text-muted-foreground flex items-center gap-1 text-sm">
                                        <Sparkles className="size-3.5" /> Generating AI
                                        summary…
                                    </p>
                                )
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
                                <SelectTrigger className="w-full" aria-label="Status">
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
                                aria-label="Resolution"
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
