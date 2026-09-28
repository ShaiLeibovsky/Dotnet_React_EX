import { Outlet, useNavigation } from 'react-router-dom'
import { Header } from '@/components/layout/Header'
import { LoadingBar } from '@/components/layout/LoadingBar'

export const AppLayout = () => {
    const navigation = useNavigation()

    return (
        <div className="min-h-screen">
            {navigation.state === 'loading' && <LoadingBar />}
            <Header />
            <main>
                <Outlet />
            </main>
        </div>
    )
}
