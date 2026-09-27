import { createBrowserRouter, Outlet } from 'react-router-dom'
import { Header } from '@/components/layout/Header'
import { TicketsPage } from '@/pages/TicketsPage'
import { TicketDetailPage, TicketLoadError, ticketLoader } from '@/pages/TicketDetailPage'
import { LoginPage } from '@/pages/LoginPage'

const AppLayout = () => (
    <div className="min-h-screen">
        <Header />
        <main>
            <Outlet />
        </main>
    </div>
)

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
