import { createBrowserRouter } from 'react-router-dom'
import { AppLayout } from '@/components/layout/AppLayout'
import { TicketLoadError } from '@/components/tickets/TicketLoadError'
import { TicketsLoadError } from '@/components/tickets/TicketsLoadError'
import { ticketLoader } from '@/loaders/ticketLoader'
import { ticketsLoader } from '@/loaders/ticketsLoader'
import { TicketsPage } from '@/pages/ticketsPage/TicketsPage'
import { TicketDetailPage } from '@/pages/ticketDetailPage/TicketDetailPage'
import { LoginPage } from '@/pages/loginPage/LoginPage'

export const router = createBrowserRouter([
    {
        element: <AppLayout />,
        children: [
            {
                path: '/',
                element: <TicketsPage />,
                loader: ticketsLoader,
                errorElement: <TicketsLoadError />,
            },
            {
                path: '/tickets/:id',
                element: <TicketDetailPage />,
                loader: ticketLoader,
                errorElement: <TicketLoadError />,
            },
            { path: '/login', element: <LoginPage /> },
        ],
    },
])
