import React from 'react'
import ReactDOM from 'react-dom/client'
import { RouterProvider } from 'react-router-dom'
import { router } from '@/router'
import { AuthProvider } from '@/context/AuthContext'
import { Toaster } from '@/components/ui/sonner'
import '@/index.css'

const rootEl = document.getElementById('root')
if (!rootEl) throw new Error('Root element #root not found')

ReactDOM.createRoot(rootEl).render(
    <React.StrictMode>
        <AuthProvider>
            <RouterProvider router={router} />
            <Toaster richColors />
        </AuthProvider>
    </React.StrictMode>,
)
