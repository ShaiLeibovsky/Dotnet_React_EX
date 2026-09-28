import React from 'react'
import ReactDOM from 'react-dom/client'
import { RouterProvider } from 'react-router-dom'
import { router } from '@/router'
import { AuthProvider } from '@/context/AuthContext'
import { LoadingBar } from '@/components/layout/LoadingBar'
import { Toaster } from '@/components/ui/sonner'
import '@/index.css'

const rootEl = document.getElementById('root')
if (!rootEl) throw new Error('Root element #root not found')

ReactDOM.createRoot(rootEl).render(
    <React.StrictMode>
        <AuthProvider>
            <RouterProvider router={router} fallbackElement={<LoadingBar />} />
            <Toaster richColors />
        </AuthProvider>
    </React.StrictMode>,
)
