import { useRevalidator } from 'react-router-dom'
import { ErrorRetry } from '@/components/tickets/ErrorRetry'

export const TicketsLoadError = () => {
    const revalidator = useRevalidator()

    return (
        <div className="mx-auto max-w-5xl px-4 py-8">
            <ErrorRetry
                message="Could not load tickets. The server is unreachable."
                onRetry={() => revalidator.revalidate()}
            />
        </div>
    )
}
