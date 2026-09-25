// Small presentation helpers shared across pages/components.

export function shortId(id: string): string {
    return '#' + id.slice(0, 7)
}

export function formatDate(iso: string): string {
    return new Date(iso).toLocaleDateString(undefined, {
        month: 'short',
        day: 'numeric',
        year: 'numeric',
    })
}

export function formatDateTime(iso: string): string {
    return new Date(iso).toLocaleString()
}
