import { ReactNode } from 'react'
import { useRouter } from 'next/router'

const navItems = [
  { label: 'Overview', path: '/dashboard' },
  { label: 'Keys', path: '/dashboard/keys' },
  { label: 'Users', path: '/dashboard/users' },
]

export default function Layout({ children }: { children: ReactNode }) {
  const router = useRouter()

  const logout = () => {
    localStorage.removeItem('token')
    router.push('/')
  }

  return (
    <div style={{ display: 'flex', minHeight: '100vh' }}>
      <nav style={{ width: 220, background: 'var(--surface)', borderRight: '1px solid var(--border)', padding: 20, display: 'flex', flexDirection: 'column' }}>
        <div style={{ fontSize: 18, fontWeight: 700, marginBottom: 32, padding: '0 8px' }}>BloxHive</div>
        {navItems.map(item => (
          <button
            key={item.path}
            onClick={() => router.push(item.path)}
            style={{
              background: router.pathname === item.path ? 'rgba(99,102,241,0.1)' : 'transparent',
              color: router.pathname === item.path ? 'var(--accent)' : 'var(--text)',
              border: 'none',
              borderRadius: 8,
              padding: '10px 12px',
              fontSize: 13,
              fontWeight: 500,
              cursor: 'pointer',
              textAlign: 'left',
              marginBottom: 4,
            }}
          >
            {item.label}
          </button>
        ))}
        <div style={{ flex: 1 }} />
        <button onClick={logout} className="btn btn-ghost" style={{ width: '100%', textAlign: 'center' }}>
          Logout
        </button>
      </nav>
      <main style={{ flex: 1, padding: 32, maxWidth: 1200 }}>{children}</main>
    </div>
  )
}
