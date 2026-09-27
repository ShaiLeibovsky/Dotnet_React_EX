import { createBrowserRouter } from 'react-router-dom'
import { AppLayout } from '@/components/layout/AppLayout'
import { TicketLoadError } from '@/components/tickets/TicketLoadError'
import { ticketLoader } from '@/loaders/ticketLoader'
import { TicketsPage } from '@/pages/TicketsPage'
import { TicketDetailPage } from '@/pages/TicketDetailPage'
import { LoginPage } from '@/pages/LoginPage'

export const router = createBrowserRouter([
    {
        element: <AppLayout />,
        children: [
            { path: '/', element: <TicketsPage /> },
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
