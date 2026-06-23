import { useEffect, useMemo, useState } from 'react'
import { useNavigate } from 'react-router-dom'
import { CircleDot, MessageSquare } from 'lucide-react'
import { Input } from '@/components/ui/input'
import {
    Select,
    SelectContent,
    SelectItem,
    SelectTrigger,
    SelectValue,
} from '@/components/ui/select'
import {
    Table,
    TableBody,
    TableCell,
    TableHead,
    TableHeader,
    TableRow,
} from '@/components/ui/table'
import { StatusBadge } from '@/components/tickets/StatusBadge'
import { NewTicketDialog } from '@/components/tickets/NewTicketDialog'
import { listTickets, isUsingFallback } from '@/api/ticketsApi'
import { shortId, formatDate } from '@/lib/format'
import { STATUSES, type Ticket } from '@/types/ticket'

type StatusFilter = 'All' | Ticket['status']

export const TicketsPage = () => {
    const [tickets, setTickets] = useState<Ticket[]>([])
    const [loading, setLoading] = useState(true)
    const [statusFilter, setStatusFilter] = useState<StatusFilter>('All')
    const [query, setQuery] = useState('')
    const navigate = useNavigate()

    useEffect(() => {
        let alive = true
        listTickets().then((data) => {
            if (!alive) return
            setTickets(data)
            setLoading(false)
        })
        return () => {
            alive = false
        }
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
        (t) => t.status === 'New' || t.status === 'In Progress',
    ).length

    return (
        <div className="mx-auto max-w-5xl px-4 py-8">
            <div className="mb-4 flex items-center gap-3">
                <h1 className="text-xl font-semibold">Support Tickets</h1>
                <div className="flex-1" />
                <NewTicketDialog
                    onCreated={(ticket) => {
                        setTickets((prev) => [ticket, ...prev])
                        navigate(`/tickets/${ticket.id}`)
                    }}
                />
            </div>

            {isUsingFallback() && (
                <div className="mb-4 rounded-md border border-amber-300 bg-amber-50 px-3 py-2 text-sm text-amber-900">
                    Backend not reachable — showing in-memory demo data. Changes won’t
                    persist.
                </div>
            )}

            <div className="mb-4 flex gap-2">
                <Select
                    value={statusFilter}
                    onValueChange={(v) => setStatusFilter(v as StatusFilter)}
                >
                    <SelectTrigger className="w-44">
                        <SelectValue placeholder="Status" />
                    </SelectTrigger>
                    <SelectContent>
                        <SelectItem value="All">All statuses</SelectItem>
                        {STATUSES.map((s) => (
                            <SelectItem key={s} value={s}>
                                {s}
                            </SelectItem>
                        ))}
                    </SelectContent>
                </Select>
                <Input
                    className="flex-1"
                    placeholder="Search by name or description…"
                    value={query}
                    onChange={(e) => setQuery(e.target.value)}
                />
            </div>

            <div className="rounded-md border">
                <div className="bg-muted/50 flex items-center gap-2 border-b px-4 py-2 text-sm font-medium">
                    <CircleDot className="size-4 text-green-600" />
                    <span>{openCount} Open</span>
                    <span className="text-muted-foreground font-normal">
                        · {filtered.length} total
                    </span>
                </div>
                <Table>
                    <TableHeader>
                        <TableRow>
                            <TableHead>Ticket</TableHead>
                            <TableHead className="w-32 text-center">Replies</TableHead>
                            <TableHead className="w-40 text-right">Status</TableHead>
                        </TableRow>
                    </TableHeader>
                    <TableBody>
                        {loading ? (
                            <TableRow>
                                <TableCell
                                    colSpan={3}
                                    className="text-muted-foreground py-10 text-center"
                                >
                                    Loading tickets…
                                </TableCell>
                            </TableRow>
                        ) : filtered.length === 0 ? (
                            <TableRow>
                                <TableCell
                                    colSpan={3}
                                    className="text-muted-foreground py-10 text-center"
                                >
                                    No tickets match your filters.
                                </TableCell>
                            </TableRow>
                        ) : (
                            filtered.map((t) => (
                                <TableRow
                                    key={t.id}
                                    className="cursor-pointer"
                                    onClick={() => navigate(`/tickets/${t.id}`)}
                                >
                                    <TableCell>
                                        <div className="font-medium">{t.description}</div>
                                        <div className="text-muted-foreground text-xs">
                                            <span className="font-mono">
                                                {shortId(t.id)}
                                            </span>{' '}
                                            opened {formatDate(t.createdAt)} by {t.name}
                                        </div>
                                        {t.summary && (
                                            <div className="text-muted-foreground mt-1 truncate text-xs">
                                                🤖 {t.summary}
                                            </div>
                                        )}
                                    </TableCell>
                                    <TableCell className="text-center">
                                        <span className="text-muted-foreground inline-flex items-center gap-1 text-sm">
                                            <MessageSquare className="size-4" />
                                            {t.responses.length}
                                        </span>
                                    </TableCell>
                                    <TableCell className="text-right">
                                        <StatusBadge status={t.status} />
                                    </TableCell>
                                </TableRow>
                            ))
                        )}
                    </TableBody>
                </Table>
            </div>
        </div>
    )
}
