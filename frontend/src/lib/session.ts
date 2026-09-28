import type { AdminSession } from '@/types/session'

const STORAGE_KEY = 'admin_session'

export const session = {
    read(): AdminSession | null {
        const stored = localStorage.getItem(STORAGE_KEY)
        return stored ? (JSON.parse(stored) as AdminSession) : null
    },
    save(admin: AdminSession): void {
        localStorage.setItem(STORAGE_KEY, JSON.stringify(admin))
    },
    clear(): void {
        localStorage.removeItem(STORAGE_KEY)
    },
}
