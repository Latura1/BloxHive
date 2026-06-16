import { useEffect, useState } from 'react'
import { useRouter } from 'next/router'
import Layout from '../../components/Layout'

const API = process.env.NEXT_PUBLIC_API_URL || 'http://localhost:5000/api'

interface OnlineUser {
  id: number
  username: string
  lastVerifiedAt: string
}

interface Stats {
  totalUsers: number
  activeUsers: number
  expiredUsers: number
  totalKeys: number
  usedKeys: number
  onlineUsers: number
  onlineUserList: OnlineUser[]
}

export default function Dashboard() {
  const [stats, setStats] = useState<Stats | null>(null)
  const [loading, setLoading] = useState(true)
  const router = useRouter()

  useEffect(() => {
    const token = localStorage.getItem('token')
    if (!token) { router.push('/'); return }

    fetch(`${API}/admin/stats`, {
      headers: { Authorization: `Bearer ${token}` },
    }).then(r => {
      if (!r.ok) { localStorage.removeItem('token'); router.push('/'); return }
      return r.json()
    }).then(d => {
      setStats(d)
      setLoading(false)
    })
  }, [])

  if (loading) return <Layout><p style={{ color: 'var(--text2)' }}>Loading...</p></Layout>

  const cards = [
    { label: 'Total Users', value: stats?.totalUsers ?? 0, color: 'var(--accent)' },
    { label: 'Active', value: stats?.activeUsers ?? 0, color: 'var(--success)' },
    { label: 'Online Now', value: stats?.onlineUsers ?? 0, color: '#22c55e' },
    { label: 'Expired', value: stats?.expiredUsers ?? 0, color: 'var(--danger)' },
    { label: 'Keys Used', value: `${stats?.usedKeys ?? 0} / ${stats?.totalKeys ?? 0}`, color: 'var(--warning)' },
  ]

  return (
    <Layout>
      <h1 style={{ fontSize: 22, fontWeight: 700, margin: '0 0 24px 0' }}>Overview</h1>
      <div style={{ display: 'grid', gridTemplateColumns: 'repeat(auto-fit, minmax(180px, 1fr))', gap: 16 }}>
        {cards.map(c => (
          <div key={c.label} className="card">
            <div style={{ color: 'var(--text2)', fontSize: 12, fontWeight: 600, textTransform: 'uppercase', letterSpacing: '0.05em' }}>{c.label}</div>
            <div style={{ fontSize: 32, fontWeight: 700, color: c.color, marginTop: 8 }}>{c.value}</div>
          </div>
        ))}
      </div>

      {stats && stats.onlineUserList && stats.onlineUserList.length > 0 && (
        <div className="card" style={{ marginTop: 24 }}>
          <h2 style={{ fontSize: 14, fontWeight: 600, margin: '0 0 12px 0', color: '#22c55e' }}>
            Online Users ({stats.onlineUsers})
          </h2>
          <div style={{ display: 'flex', flexDirection: 'column', gap: 6 }}>
            {stats.onlineUserList.map(u => (
              <div key={u.id} style={{ display: 'flex', justifyContent: 'space-between', fontSize: 13, padding: '4px 0', borderBottom: '1px solid var(--border)' }}>
                <span style={{ fontWeight: 600 }}>{u.username}</span>
                <span style={{ color: 'var(--text2)' }}>Last verify: {new Date(u.lastVerifiedAt).toLocaleTimeString()}</span>
              </div>
            ))}
          </div>
        </div>
      )}
    </Layout>
  )
}
