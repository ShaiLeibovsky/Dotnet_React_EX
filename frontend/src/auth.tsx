// Lightweight client auth for the bonus Admin-login feature.
// Only logged-in users may EDIT tickets; anyone may CREATE/VIEW.
// Replace the fake login with a real /api/auth/login + JWT call later.
import { createContext, useContext, useState, type ReactNode } from 'react'

export interface AuthUser {
  email: string
  token: string
}

interface AuthContextValue {
  user: AuthUser | null
  isAdmin: boolean
  login: (email: string, password: string) => Promise<AuthUser>
  logout: () => void
}

const AuthContext = createContext<AuthContextValue | null>(null)

export function AuthProvider({ children }: { children: ReactNode }) {
  const [user, setUser] = useState<AuthUser | null>(() => {
    const raw = localStorage.getItem('auth_user')
    return raw ? (JSON.parse(raw) as AuthUser) : null
  })

  async function login(email: string, password: string): Promise<AuthUser> {
    // TODO: POST /api/auth/login -> { token } and store JWT.
    if (!email || !password) throw new Error('Email and password required')
    const u: AuthUser = { email, token: 'demo-token' }
    localStorage.setItem('auth_user', JSON.stringify(u))
    setUser(u)
    return u
  }

  function logout(): void {
    localStorage.removeItem('auth_user')
    setUser(null)
  }

  return (
    <AuthContext.Provider value={{ user, login, logout, isAdmin: !!user }}>
      {children}
    </AuthContext.Provider>
  )
}

export function useAuth(): AuthContextValue {
  const ctx = useContext(AuthContext)
  if (!ctx) throw new Error('useAuth must be used within an AuthProvider')
  return ctx
}
