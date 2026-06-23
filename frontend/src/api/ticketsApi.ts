// All ticket I/O goes through here. Frontend never touches the JSON file
// directly — only these API endpoints (per the exercise spec).
//
// If the backend is not running yet, calls fall back to in-memory mock
// data so the UI stays usable during frontend development.
import { mockTickets } from '@/data/mockTickets'
import { uuid } from '@/lib/format'
import type {
  AddResponseInput,
  CreateTicketInput,
  Ticket,
  UpdateTicketInput,
} from '@/types/ticket'

const BASE = '/api'

// In-memory fallback store (seeded from mock data).
let fallback: Ticket[] = mockTickets.map((t) => ({ ...t, responses: [...t.responses] }))
let useFallback = false

async function tryFetch<T>(path: string, options?: RequestInit): Promise<T | null> {
  const res = await fetch(`${BASE}${path}`, {
    headers: { 'Content-Type': 'application/json' },
    ...options,
  })
  if (!res.ok) throw new Error(`API ${res.status}`)
  if (res.status === 204) return null
  return (await res.json()) as T
}

export async function listTickets(): Promise<Ticket[]> {
  if (useFallback) return [...fallback]
  try {
    return (await tryFetch<Ticket[]>('/tickets')) ?? []
  } catch {
    useFallback = true
    return [...fallback]
  }
}

export async function getTicket(id: string): Promise<Ticket | null> {
  if (useFallback) return fallback.find((t) => t.id === id) ?? null
  try {
    return await tryFetch<Ticket>(`/tickets/${id}`)
  } catch {
    useFallback = true
    return fallback.find((t) => t.id === id) ?? null
  }
}

export async function createTicket(input: CreateTicketInput): Promise<Ticket> {
  if (!useFallback) {
    try {
      const created = await tryFetch<Ticket>('/tickets', {
        method: 'POST',
        body: JSON.stringify(input),
      })
      if (created) return created
    } catch {
      useFallback = true
    }
  }
  const now = new Date().toISOString()
  const ticket: Ticket = {
    id: uuid(),
    name: input.name,
    email: input.email,
    description: input.description,
    summary: '',
    status: 'New',
    resolution: '',
    responses: [],
    createdAt: now,
    updatedAt: now,
  }
  fallback = [ticket, ...fallback]
  return ticket
}

export async function updateTicket(id: string, patch: UpdateTicketInput): Promise<Ticket> {
  if (!useFallback) {
    try {
      const updated = await tryFetch<Ticket>(`/tickets/${id}`, {
        method: 'PUT',
        body: JSON.stringify(patch),
      })
      if (updated) return updated
    } catch {
      useFallback = true
    }
  }
  fallback = fallback.map((t) =>
    t.id === id ? { ...t, ...patch, updatedAt: new Date().toISOString() } : t
  )
  return requireTicket(id)
}

export async function addResponse(id: string, input: AddResponseInput): Promise<Ticket> {
  if (!useFallback) {
    try {
      const updated = await tryFetch<Ticket>(`/tickets/${id}/responses`, {
        method: 'POST',
        body: JSON.stringify(input),
      })
      if (updated) return updated
    } catch {
      useFallback = true
    }
  }
  const now = new Date().toISOString()
  fallback = fallback.map((t) =>
    t.id === id
      ? {
          ...t,
          responses: [...t.responses, { id: uuid(), ...input, createdAt: now }],
          updatedAt: now,
        }
      : t
  )
  return requireTicket(id)
}

function requireTicket(id: string): Ticket {
  const found = fallback.find((t) => t.id === id)
  if (!found) throw new Error(`Ticket ${id} not found`)
  return found
}

// Flag exposed so the UI can show a "demo data" notice.
export function isUsingFallback(): boolean {
  return useFallback
}
