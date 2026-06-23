import { useState, type FormEvent } from 'react'
import { useNavigate } from 'react-router-dom'
import { MarkGithubIcon } from '../components/icons'
import { useAuth } from '../auth'

export default function LoginPage() {
  const { login } = useAuth()
  const navigate = useNavigate()
  const [email, setEmail] = useState('')
  const [password, setPassword] = useState('')
  const [error, setError] = useState('')
  const [busy, setBusy] = useState(false)

  async function submit(e: FormEvent) {
    e.preventDefault()
    setError('')
    setBusy(true)
    try {
      await login(email, password)
      navigate('/')
    } catch (err) {
      setError(err instanceof Error ? err.message : 'Login failed')
    } finally {
      setBusy(false)
    }
  }

  return (
    <div className="login-wrap">
      <div className="logo"><MarkGithubIcon size={40} color="#1f2328" /></div>
      <h1 style={{ fontWeight: 300, fontSize: 24 }}>Sign in to SupportHub</h1>
      <form className="login-card" onSubmit={submit}>
        <div className="field">
          <label htmlFor="lg-email">Email address</label>
          <input id="lg-email" className="input" value={email}
            onChange={(e) => setEmail(e.target.value)} autoComplete="username" />
        </div>
        <div className="field">
          <label htmlFor="lg-pass">Password</label>
          <input id="lg-pass" type="password" className="input" value={password}
            onChange={(e) => setPassword(e.target.value)} autoComplete="current-password" />
        </div>
        {error && <div className="error" style={{ marginBottom: 12 }}>{error}</div>}
        <button className="btn btn-primary" style={{ width: '100%' }} disabled={busy}>
          {busy ? 'Signing in…' : 'Sign in'}
        </button>
      </form>
      <p style={{ color: 'var(--fg-muted)', fontSize: 13, marginTop: 16 }}>
        Admin only. Any email + password works in this demo.
      </p>
    </div>
  )
}
