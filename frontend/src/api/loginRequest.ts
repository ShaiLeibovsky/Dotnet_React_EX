import type { AdminSession } from '@/types/session'

export async function loginRequest(
    email: string,
    password: string,
): Promise<AdminSession> {
    const response = await fetch('/api/auth/login', {
        method: 'POST',
        headers: { 'Content-Type': 'application/json' },
        body: JSON.stringify({ email, password }),
    })
    if (response.status === 401) throw new Error('Email or password is incorrect.')
    if (!response.ok) throw new Error('Sign in failed. Please try again.')
    return (await response.json()) as AdminSession
}
