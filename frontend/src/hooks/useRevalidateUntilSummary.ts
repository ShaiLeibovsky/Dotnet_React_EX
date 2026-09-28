import { useEffect } from 'react'
import { useRevalidator } from 'react-router-dom'
import type { Ticket } from '@/types/ticket'

// ADR-0003 section 6, waiting for the summary in the browser.
const pollIntervalMs = 2_000
const giveUpAfterMs = 60_000

export const useRevalidateUntilSummary = (ticket: Ticket) => {
    const { revalidate } = useRevalidator()
    const summaryStillExpected =
        !ticket.summary && Date.now() - Date.parse(ticket.createdAt) < giveUpAfterMs

    useEffect(() => {
        if (!summaryStillExpected) return
        const poll = setInterval(revalidate, pollIntervalMs)
        return () => clearInterval(poll)
    }, [summaryStillExpected, revalidate])
}
