import { session } from '@/lib/session'
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
    const token = session.read()?.token
    const response = await fetch(`${BASE}${path}`, {
        ...options,
        headers: {
            'Content-Type': 'application/json',
            ...(token ? { Authorization: `Bearer ${token}` } : {}),
            ...options?.headers,
        },
    })
    if (response.status === 401 && token) {
        session.clear()
        window.location.assign('/login')
    }
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
