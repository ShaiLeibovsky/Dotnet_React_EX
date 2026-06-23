import { Link, useNavigate } from 'react-router-dom'
import { LifeBuoy, LogOut } from 'lucide-react'
import { Avatar, AvatarFallback } from '@/components/ui/avatar'
import { Button } from '@/components/ui/button'
import { useAuth } from '@/context/AuthContext'

export function Header() {
  const { user, logout } = useAuth()
  const navigate = useNavigate()

  return (
    <header className="bg-neutral-900 text-white">
      <div className="mx-auto flex h-14 max-w-5xl items-center gap-2 px-4">
        <Link to="/" className="flex items-center gap-2 font-semibold">
          <LifeBuoy className="size-6" />
          <span>SupportHub</span>
          <span className="font-normal text-neutral-400">/ tickets</span>
        </Link>
        <div className="flex-1" />
        {user ? (
          <div className="flex items-center gap-3">
            <Button
              variant="ghost"
              size="sm"
              className="text-neutral-300 hover:bg-neutral-800 hover:text-white"
              onClick={() => {
                logout()
                navigate('/')
              }}
            >
              <LogOut className="size-4" />
              Sign out
            </Button>
            <Avatar className="size-8" title={user.email}>
              <AvatarFallback className="bg-neutral-700 text-white">
                {user.email[0].toUpperCase()}
              </AvatarFallback>
            </Avatar>
          </div>
        ) : (
          <Button
            asChild
            variant="ghost"
            size="sm"
            className="text-neutral-300 hover:bg-neutral-800 hover:text-white"
          >
            <Link to="/login">Sign in</Link>
          </Button>
        )}
      </div>
    </header>
  )
}
