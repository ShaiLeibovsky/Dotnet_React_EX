import { Button } from '@/components/ui/button'

interface Props {
    message: string
    onRetry: () => void
}

export const ErrorRetry = ({ message, onRetry }: Props) => (
    <div className="flex items-center gap-3 rounded-md border border-red-300 bg-red-50 px-3 py-2 text-sm text-red-900">
        <span className="flex-1">{message}</span>
        <Button size="sm" variant="outline" onClick={onRetry}>
            Retry
        </Button>
    </div>
)
