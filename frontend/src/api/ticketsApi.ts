import type { CreateTicketInput, Ticket, UpdateTicketInput } from '@/types/ticket'

const BASE = '/api'

export class ApiError extends Error {
    constructor(
        readonly status: number,
        path: string,
    ) {
        super(`Request to ${path} failed with status ${status}`)
    }
}

async function request<T>(path: string, options?: RequestInit): Promise<T> {
    const response = await fetch(`${BASE}${path}`, {
        headers: { 'Content-Type': 'application/json' },
        ...options,
    })
    if (!response.ok) throw new ApiError(response.status, path)
    return (await response.json()) as T
}

export function listTickets(): Promise<Ticket[]> {
    return request<Ticket[]>('/tickets')
}

export function getTicket(id: string): Promise<Ticket> {
    return request<Ticket>(`/tickets/${id}`)
}

export function createTicket(input: CreateTicketInput): Promise<Ticket> {
    return request<Ticket>('/tickets', {
        method: 'POST',
        body: JSON.stringify(input),
    })
}

export function updateTicket(id: string, patch: UpdateTicketInput): Promise<Ticket> {
    return request<Ticket>(`/tickets/${id}`, {
        method: 'PUT',
        body: JSON.stringify(patch),
    })
}
