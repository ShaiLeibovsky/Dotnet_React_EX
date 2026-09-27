import { Link, useRevalidator, useRouteError } from 'react-router-dom'
import { ArrowLeft } from 'lucide-react'
import { Button } from '@/components/ui/button'
import { ErrorRetry } from '@/components/tickets/ErrorRetry'
import { ApiError } from '@/api/ticketsApi'

export const TicketLoadError = () => {
    const error = useRouteError()
    const revalidator = useRevalidator()
    const message =
        error instanceof ApiError && error.status === 404
            ? 'No ticket exists with this id.'
            : 'Could not load this ticket. The server is unreachable.'

    return (
        <div className="mx-auto max-w-5xl px-4 py-8">
            <ErrorRetry message={message} onRetry={() => revalidator.revalidate()} />
            <Button asChild variant="link" className="mt-4 px-0">
                <Link to="/">
                    <ArrowLeft className="size-4" /> All tickets
                </Link>
            </Button>
        </div>
    )
}
