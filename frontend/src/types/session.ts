// Mirrors the backend AdminSessionDto returned by POST /api/auth/login.

export interface AdminSession {
    token: string
    email: string
}
