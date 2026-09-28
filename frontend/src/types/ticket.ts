// Shared domain types — mirror the backend Ticket entity / DTOs.

export const STATUSES = ['New', 'In Progress', 'Resolved', 'Closed'] as const
export type TicketStatus = (typeof STATUSES)[number]

export interface Ticket {
    id: string
    name: string
    email: string
    description: string
    summary: string
    status: TicketStatus
    resolution: string
    imageUrl: string
    createdAt: string
    updatedAt: string
}

// ADR-0003 section 1, what counts as an acceptable image
export const MAX_IMAGE_BYTES = 5 * 1024 * 1024

// Payload for POST /api/tickets
export interface CreateTicketInput {
    name: string
    email: string
    description: string
    image?: File
}

// Payload for PUT /api/tickets/{id}
export interface UpdateTicketInput {
    status: TicketStatus
    resolution: string
}
