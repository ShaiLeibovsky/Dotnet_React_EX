import { useState, type FormEvent } from 'react'
import { useNavigate } from 'react-router-dom'
import { LifeBuoy } from 'lucide-react'
import { Button } from '@/components/ui/button'
import { Card, CardContent } from '@/components/ui/card'
import { Input } from '@/components/ui/input'
import { Label } from '@/components/ui/label'
import { useAuth } from '@/context/AuthContext'

export function LoginPage() {
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
        <div className="mx-auto max-w-sm px-4 py-12 text-center">
            <LifeBuoy className="mx-auto mb-4 size-10" />
            <h1 className="mb-6 text-2xl font-light">Sign in to SupportHub</h1>
            <Card>
                <CardContent className="pt-6">
                    <form onSubmit={submit} className="space-y-4 text-left">
                        <div className="grid gap-2">
                            <Label htmlFor="lg-email">Email address</Label>
                            <Input
                                id="lg-email"
                                value={email}
                                onChange={(e) => setEmail(e.target.value)}
                                autoComplete="username"
                            />
                        </div>
                        <div className="grid gap-2">
                            <Label htmlFor="lg-pass">Password</Label>
                            <Input
                                id="lg-pass"
                                type="password"
                                value={password}
                                onChange={(e) => setPassword(e.target.value)}
                                autoComplete="current-password"
                            />
                        </div>
                        {error && <p className="text-destructive text-sm">{error}</p>}
                        <Button type="submit" className="w-full" disabled={busy}>
                            {busy ? 'Signing in…' : 'Sign in'}
                        </Button>
                    </form>
                </CardContent>
            </Card>
            <p className="text-muted-foreground mt-4 text-sm">
                Admin login POC — any email + password works in this demo.
            </p>
        </div>
    )
}
