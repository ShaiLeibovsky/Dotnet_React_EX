import { useState, type FormEvent } from 'react'
import { Plus } from 'lucide-react'
import { Button } from '@/components/ui/button'
import { Input } from '@/components/ui/input'
import { Label } from '@/components/ui/label'
import { Textarea } from '@/components/ui/textarea'
import {
    Dialog,
    DialogClose,
    DialogContent,
    DialogDescription,
    DialogFooter,
    DialogHeader,
    DialogTitle,
    DialogTrigger,
} from '@/components/ui/dialog'
import { createTicket } from '@/api/ticketsApi'
import type { Ticket } from '@/types/ticket'

const emailRe = /^[^\s@]+@[^\s@]+\.[^\s@]+$/

interface Errors {
    name?: string
    email?: string
    description?: string
}

export function NewTicketDialog({ onCreated }: { onCreated: (ticket: Ticket) => void }) {
    const [open, setOpen] = useState(false)
    const [name, setName] = useState('')
    const [email, setEmail] = useState('')
    const [description, setDescription] = useState('')
    const [errors, setErrors] = useState<Errors>({})
    const [saving, setSaving] = useState(false)

    function reset() {
        setName('')
        setEmail('')
        setDescription('')
        setErrors({})
    }

    function validate(): boolean {
        const e: Errors = {}
        if (!name.trim()) e.name = 'Full name is required'
        if (!email.trim()) e.email = 'Email is required'
        else if (!emailRe.test(email)) e.email = 'Enter a valid email'
        if (!description.trim()) e.description = 'Issue description is required'
        setErrors(e)
        return Object.keys(e).length === 0
    }

    async function submit(ev: FormEvent) {
        ev.preventDefault()
        if (!validate()) return
        setSaving(true)
        try {
            const ticket = await createTicket({
                name: name.trim(),
                email: email.trim(),
                description: description.trim(),
            })
            onCreated(ticket)
            setOpen(false)
            reset()
        } finally {
            setSaving(false)
        }
    }

    return (
        <Dialog
            open={open}
            onOpenChange={(o) => {
                setOpen(o)
                if (!o) reset()
            }}
        >
            <DialogTrigger asChild>
                <Button>
                    <Plus className="size-4" />
                    New ticket
                </Button>
            </DialogTrigger>
            <DialogContent>
                <form onSubmit={submit}>
                    <DialogHeader>
                        <DialogTitle>Open a new ticket</DialogTitle>
                        <DialogDescription>
                            Describe your issue. Our support team will respond on this
                            ticket.
                        </DialogDescription>
                    </DialogHeader>

                    <div className="grid gap-4 py-4">
                        <div className="grid gap-2">
                            <Label htmlFor="nt-name">Full name</Label>
                            <Input
                                id="nt-name"
                                value={name}
                                onChange={(e) => setName(e.target.value)}
                                placeholder="Jane Smith"
                            />
                            {errors.name && (
                                <p className="text-destructive text-sm">{errors.name}</p>
                            )}
                        </div>
                        <div className="grid gap-2">
                            <Label htmlFor="nt-email">Email address</Label>
                            <Input
                                id="nt-email"
                                value={email}
                                onChange={(e) => setEmail(e.target.value)}
                                placeholder="jane@example.com"
                            />
                            {errors.email && (
                                <p className="text-destructive text-sm">{errors.email}</p>
                            )}
                        </div>
                        <div className="grid gap-2">
                            <Label htmlFor="nt-desc">Issue description</Label>
                            <Textarea
                                id="nt-desc"
                                value={description}
                                onChange={(e) => setDescription(e.target.value)}
                                placeholder="Describe what went wrong…"
                                rows={4}
                            />
                            {errors.description && (
                                <p className="text-destructive text-sm">
                                    {errors.description}
                                </p>
                            )}
                        </div>
                    </div>

                    <DialogFooter>
                        <DialogClose asChild>
                            <Button type="button" variant="outline">
                                Cancel
                            </Button>
                        </DialogClose>
                        <Button type="submit" disabled={saving}>
                            {saving ? 'Submitting…' : 'Submit ticket'}
                        </Button>
                    </DialogFooter>
                </form>
            </DialogContent>
        </Dialog>
    )
}
