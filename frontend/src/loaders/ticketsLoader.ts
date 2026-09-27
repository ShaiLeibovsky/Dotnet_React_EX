import { listTickets } from '@/api/ticketsApi'
import type { Ticket } from '@/types/ticket'

export const ticketsLoader = (): Promise<Ticket[]> => listTickets()
