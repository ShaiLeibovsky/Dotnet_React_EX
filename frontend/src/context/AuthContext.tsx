import { createContext, useContext, useState, type ReactNode } from 'react'
import { loginRequest } from '@/api/loginRequest'
import { session } from '@/lib/session'
import type { AdminSession } from '@/types/session'

interface AuthContextValue {
    user: AdminSession | null
    isAdmin: boolean
    login: (email: string, password: string) => Promise<AdminSession>
    logout: () => void
}

const AuthContext = createContext<AuthContextValue | null>(null)

export const AuthProvider = ({ children }: { children: ReactNode }) => {
    const [user, setUser] = useState<AdminSession | null>(session.read)

    async function login(email: string, password: string): Promise<AdminSession> {
        const admin = await loginRequest(email, password)
        session.save(admin)
        setUser(admin)
        return admin
    }

    function logout(): void {
        session.clear()
        setUser(null)
    }

    return (
        <AuthContext.Provider value={{ user, login, logout, isAdmin: !!user }}>
            {children}
        </AuthContext.Provider>
    )
}

// eslint-disable-next-line react-refresh/only-export-components
export function useAuth(): AuthContextValue {
    const ctx = useContext(AuthContext)
    if (!ctx) throw new Error('useAuth must be used within an AuthProvider')
    return ctx
}
