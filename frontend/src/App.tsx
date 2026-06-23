import { Routes, Route } from 'react-router-dom'
import Header from './components/Header'
import TicketsPage from './pages/TicketsPage'
import TicketDetailPage from './pages/TicketDetailPage'
import LoginPage from './pages/LoginPage'

export default function App() {
  return (
    <>
      <Header />
      <Routes>
        <Route path="/" element={<TicketsPage />} />
        <Route path="/tickets/:id" element={<TicketDetailPage />} />
        <Route path="/login" element={<LoginPage />} />
      </Routes>
    </>
  )
}
