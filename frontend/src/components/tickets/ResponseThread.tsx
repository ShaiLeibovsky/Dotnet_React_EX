import { useState, type FormEvent } from 'react'
import { ShieldCheck, User } from 'lucide-react'
import { Avatar, AvatarFallback } from '@/components/ui/avatar'
import { Button } from '@/components/ui/button'
import { Textarea } from '@/components/ui/textarea'
import { cn } from '@/lib/utils'
import { formatDateTime } from '@/lib/format'
import type { TicketResponse } from '@/types/ticket'

interface Props {
  responses: TicketResponse[]
  canRespond: boolean
  onRespond: (body: string) => Promise<void>
}

export function ResponseThread({ responses, canRespond, onRespond }: Props) {
  const [body, setBody] = useState('')
  const [sending, setSending] = useState(false)

  async function submit(e: FormEvent) {
    e.preventDefault()
    if (!body.trim()) return
    setSending(true)
    try {
      await onRespond(body.trim())
      setBody('')
    } finally {
      setSending(false)
    }
  }

  return (
    <div className="space-y-4">
      {responses.length === 0 ? (
        <p className="text-muted-foreground text-sm">
          No responses yet. The customer is awaiting a reply.
        </p>
      ) : (
        responses.map((r) => {
          const isAdmin = r.role === 'admin'
          return (
            <div key={r.id} className="flex gap-3">
              <Avatar className="size-8">
                <AvatarFallback
                  className={cn(
                    isAdmin ? 'bg-primary text-primary-foreground' : 'bg-muted',
                  )}
                >
                  {isAdmin ? (
                    <ShieldCheck className="size-4" />
                  ) : (
                    <User className="size-4" />
                  )}
                </AvatarFallback>
              </Avatar>
              <div className="flex-1 rounded-md border">
                <div className="bg-muted/50 flex items-center gap-2 border-b px-3 py-1.5 text-sm">
                  <span className="font-medium">{r.author}</span>
                  {isAdmin && (
                    <span className="text-primary bg-primary/10 rounded-full px-2 py-0.5 text-xs">
                      Admin
                    </span>
                  )}
                  <span className="text-muted-foreground ml-auto text-xs">
                    {formatDateTime(r.createdAt)}
                  </span>
                </div>
                <p className="px-3 py-2 text-sm whitespace-pre-wrap">{r.body}</p>
              </div>
            </div>
          )
        })
      )}

      {canRespond && (
        <form onSubmit={submit} className="space-y-2 border-t pt-4">
          <Textarea
            value={body}
            onChange={(e) => setBody(e.target.value)}
            placeholder="Write a response to the customer…"
            rows={3}
          />
          <div className="flex justify-end">
            <Button type="submit" size="sm" disabled={!body.trim() || sending}>
              {sending ? 'Sending…' : 'Respond'}
            </Button>
          </div>
        </form>
      )}
    </div>
  )
}
