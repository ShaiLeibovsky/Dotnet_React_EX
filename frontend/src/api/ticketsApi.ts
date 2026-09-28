import type { CreateTicketInput, Ticket, UpdateTicketInput } from '@/types/ticket'

const BASE = '/api'

export class ApiError extends Error {
    constructor(
        readonly status: number,
        message: string,
    ) {
        super(message)
    }
}

interface ProblemDetails {
    detail?: string
    errors?: Record<string, string[]>
}

async function failureMessage(response: Response, path: string): Promise<string> {
    const problem = (await response.json().catch(() => null)) as ProblemDetails | null
    const fieldMessages = Object.values(problem?.errors ?? {}).flat()
    return (
        fieldMessages.join(' ') ||
        problem?.detail ||
        `Request to ${path} failed with status ${response.status}`
    )
}

async function request<T>(path: string, options?: RequestInit): Promise<T> {
    const response = await fetch(`${BASE}${path}`, {
        headers:
            options?.body instanceof FormData
                ? {}
                : { 'Content-Type': 'application/json' },
        ...options,
    })
    if (!response.ok)
        throw new ApiError(response.status, await failureMessage(response, path))
    return (await response.json()) as T
}

export function listTickets(): Promise<Ticket[]> {
    return request<Ticket[]>('/tickets')
}

export function getTicket(id: string): Promise<Ticket> {
    return request<Ticket>(`/tickets/${id}`)
}

export function createTicket(input: CreateTicketInput): Promise<Ticket> {
    const body = new FormData()
    body.append('name', input.name)
    body.append('email', input.email)
    body.append('description', input.description)
    if (input.image) body.append('image', input.image)
    return request<Ticket>('/tickets', { method: 'POST', body })
}

export function updateTicket(id: string, patch: UpdateTicketInput): Promise<Ticket> {
    return request<Ticket>(`/tickets/${id}`, {
        method: 'PUT',
        body: JSON.stringify(patch),
    })
}
