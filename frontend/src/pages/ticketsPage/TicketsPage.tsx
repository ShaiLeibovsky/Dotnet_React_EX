import { useMemo, useState } from 'react'
import { Link, useLoaderData, useNavigate } from 'react-router-dom'
import { CircleDot } from 'lucide-react'
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
import { shortId, formatDate } from '@/lib/format'
import { STATUSES, type Ticket } from '@/types/ticket'

type StatusFilter = 'All' | Ticket['status']

export const TicketsPage = () => {
    const tickets = useLoaderData() as Ticket[]
    const [statusFilter, setStatusFilter] = useState<StatusFilter>('All')
    const [query, setQuery] = useState('')
    const navigate = useNavigate()

    const filtered = useMemo(() => {
        const search = query.trim().toLowerCase()
        return tickets.filter((ticket) => {
            if (statusFilter !== 'All' && ticket.status !== statusFilter) return false
            if (!search) return true
            return (
                ticket.name.toLowerCase().includes(search) ||
                ticket.description.toLowerCase().includes(search)
            )
        })
    }, [tickets, statusFilter, query])

    const openCount = filtered.filter(
        (ticket) => ticket.status === 'New' || ticket.status === 'In Progress',
    ).length

    return (
        <div className="mx-auto max-w-5xl px-4 py-8">
            <div className="mb-4 flex flex-wrap items-center gap-3">
                <h1 className="text-xl font-semibold">Support Tickets</h1>
                <div className="flex-1" />
                <NewTicketDialog
                    onCreated={(ticket) => navigate(`/tickets/${ticket.id}`)}
                />
            </div>

            <div className="mb-4 flex flex-col gap-2 sm:flex-row">
                <Select
                    value={statusFilter}
                    onValueChange={(v) => setStatusFilter(v as StatusFilter)}
                >
                    <SelectTrigger
                        className="w-full sm:w-44"
                        aria-label="Filter by status"
                    >
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
                    aria-label="Search tickets by name or description"
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
                            <TableHead className="w-40 text-right">Status</TableHead>
                        </TableRow>
                    </TableHeader>
                    <TableBody>
                        {filtered.length === 0 ? (
                            <TableRow>
                                <TableCell
                                    colSpan={2}
                                    className="text-muted-foreground py-10 text-center"
                                >
                                    {tickets.length === 0
                                        ? 'No tickets yet. Open the first one.'
                                        : 'No tickets match your filters.'}
                                </TableCell>
                            </TableRow>
                        ) : (
                            filtered.map((ticket) => (
                                <TableRow
                                    key={ticket.id}
                                    className="has-[a:focus-visible]:outline-ring relative cursor-pointer has-[a:focus-visible]:-outline-offset-2 has-[a:focus-visible]:outline-2"
                                >
                                    <TableCell className="break-words whitespace-normal">
                                        <Link
                                            to={`/tickets/${ticket.id}`}
                                            className="font-medium hover:underline after:absolute after:inset-0 focus-visible:outline-none"
                                        >
                                            {ticket.description}
                                        </Link>
                                        <div className="text-muted-foreground text-xs">
                                            <span className="font-mono">
                                                {shortId(ticket.id)}
                                            </span>{' '}
                                            opened {formatDate(ticket.createdAt)} by{' '}
                                            {ticket.name}
                                        </div>
                                        {ticket.summary && (
                                            <div className="text-muted-foreground mt-1 line-clamp-2 text-xs">
                                                <span aria-hidden="true">🤖</span>
                                                <span className="sr-only">
                                                    AI summary:
                                                </span>{' '}
                                                {ticket.summary}
                                            </div>
                                        )}
                                    </TableCell>
                                    <TableCell className="align-top text-right">
                                        <StatusBadge status={ticket.status} />
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
