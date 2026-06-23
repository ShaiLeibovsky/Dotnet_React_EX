import { Link, useNavigate } from 'react-router-dom'
import { MarkGithubIcon } from './icons'
import { useAuth } from '../auth'

export default function Header() {
  const { user, logout } = useAuth()
  const navigate = useNavigate()

  return (
    <header className="appbar">
      <Link to="/" className="brand">
        <MarkGithubIcon size={28} />
        <span>SupportHub</span>
        <span style={{ color: '#7d8590', fontWeight: 400 }}>/ tickets</span>
      </Link>
      <span className="spacer" />
      {user ? (
        <>
          <span className="btn-link" onClick={() => { logout(); navigate('/') }}>
            Sign out
          </span>
          <span className="user" title={user.email}>
            {user.email[0].toUpperCase()}
          </span>
        </>
      ) : (
        <Link to="/login" className="btn-link">Sign in</Link>
      )}
    </header>
  )
}
