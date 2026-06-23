import { IssueOpenIcon, IssueClosedIcon } from './icons'
import type { TicketStatus } from '../types'

const MAP: Record<TicketStatus, { cls: string; closed: boolean }> = {
  New: { cls: 'badge-new', closed: false },
  'In Progress': { cls: 'badge-inprogress', closed: false },
  Resolved: { cls: 'badge-resolved', closed: true },
  Closed: { cls: 'badge-closed', closed: true },
}

export default function StatusBadge({ status }: { status: TicketStatus }) {
  const meta = MAP[status] ?? { cls: 'badge-closed', closed: false }
  const Icon = meta.closed ? IssueClosedIcon : IssueOpenIcon
  return (
    <span className={`badge ${meta.cls}`}>
      <Icon size={14} />
      {status}
    </span>
  )
}
