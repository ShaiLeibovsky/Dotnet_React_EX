import { Routes, Route } from 'react-router-dom'
import { Header } from '@/components/layout/Header'
import { TicketsPage } from '@/pages/TicketsPage'
import { TicketDetailPage } from '@/pages/TicketDetailPage'
import { LoginPage } from '@/pages/LoginPage'

const App = () => {
    return (
        <div className="min-h-screen">
            <Header />
            <main>
                <Routes>
                    <Route path="/" element={<TicketsPage />} />
                    <Route path="/tickets/:id" element={<TicketDetailPage />} />
                    <Route path="/login" element={<LoginPage />} />
                </Routes>
            </main>
        </div>
    )
}

export default App
