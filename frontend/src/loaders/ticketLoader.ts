import type { LoaderFunctionArgs } from 'react-router-dom'
import { getTicket } from '@/api/ticketsApi'
import type { Ticket } from '@/types/ticket'

export const ticketLoader = ({ params }: LoaderFunctionArgs): Promise<Ticket> =>
    getTicket(params.id ?? '')
