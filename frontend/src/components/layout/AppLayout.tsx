import { Outlet } from 'react-router-dom'
import { Header } from '@/components/layout/Header'

export const AppLayout = () => (
    <div className="min-h-screen">
        <Header />
        <main>
            <Outlet />
        </main>
    </div>
)
