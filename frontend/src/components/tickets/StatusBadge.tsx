import { CircleDot, CircleCheck, CircleDashed, CircleX } from 'lucide-react'
import { Badge } from '@/components/ui/badge'
import { cn } from '@/lib/utils'
import type { TicketStatus } from '@/types/ticket'

const STYLES: Record<TicketStatus, { className: string; Icon: typeof CircleDot }> = {
    New: { className: 'bg-green-600 text-white', Icon: CircleDot },
    'In Progress': { className: 'bg-amber-500 text-white', Icon: CircleDashed },
    Resolved: { className: 'bg-violet-600 text-white', Icon: CircleCheck },
    Closed: { className: 'bg-zinc-500 text-white', Icon: CircleX },
}

export const StatusBadge = ({ status }: { status: TicketStatus }) => {
    const { className, Icon } = STYLES[status]
    return (
        <Badge className={cn('gap-1 rounded-full border-transparent', className)}>
            <Icon className="size-3.5" />
            {status}
        </Badge>
    )
}
