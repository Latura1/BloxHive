import { useEffect, useState } from 'react'
import { useRouter } from 'next/router'
import Layout from '../../components/Layout'

const API = process.env.NEXT_PUBLIC_API_URL || 'http://localhost:5000/api'

interface User {
  id: number
  username: string
  createdAt: string
  expiresAt: string | null
  remainingDays: number | null
  isActive: boolean
  hasHwid: boolean
  lastLoginAt: string | null
  usedKey: string | null
  forceLogoutAt: string | null
  lastVerifiedAt: string | null
}

const FIVE_MIN = 5 * 60 * 1000

export default function Users() {
  const [users, setUsers] = useState<User[]>([])
  const [loading, setLoading] = useState(true)
  const router = useRouter()

  const token = typeof window !== 'undefined' ? localStorage.getItem('token') : null

  const load = () => {
    fetch(`${API}/admin/users`, { headers: { Authorization: `Bearer ${token}` } })
      .then(r => { if (!r.ok) { router.push('/'); return }; return r.json() })
      .then(d => { setUsers(d); setLoading(false) })
  }

  useEffect(() => { if (!token) { router.push('/'); return }; load() }, [])

  const deleteUser = async (id: number, username: string) => {
    if (!confirm(`Delete user "${username}"?`)) return
    await fetch(`${API}/admin/users/${id}`, { method: 'DELETE', headers: { Authorization: `Bearer ${token}` } })
    load()
  }

  const resetHwid = async (id: number) => {
    if (!confirm('Reset HWID for this user?')) return
    await fetch(`${API}/admin/reset-hwid/${id}`, { method: 'POST', headers: { Authorization: `Bearer ${token}` } })
    load()
  }

  const forceLogout = async (id: number) => {
    if (!confirm('Force logout this user?')) return
    await fetch(`${API}/admin/force-logout/${id}`, { method: 'POST', headers: { Authorization: `Bearer ${token}` } })
    load()
  }

  const isOnline = (lastVerified: string | null) => {
    if (!lastVerified) return false
    return Date.now() - new Date(lastVerified).getTime() < FIVE_MIN
  }

  const remainingBadge = (days: number | null) => {
    if (days === null) return <span className="badge badge-green">Permanent</span>
    if (days <= 0) return <span className="badge badge-red">Expired</span>
    if (days <= 7) return <span className="badge badge-yellow">{days}d</span>
    return <span className="badge badge-green">{days}d</span>
  }

  return (
    <Layout>
      <h1 style={{ fontSize: 22, fontWeight: 700, margin: '0 0 24px 0' }}>Users</h1>

      {loading ? <p style={{ color: 'var(--text2)' }}>Loading...</p> : (
        <div className="card" style={{ padding: 0, overflow: 'auto' }}>
          <table>
            <thead>
              <tr>
                <th>Username</th>
                <th>Key</th>
                <th>Remaining</th>
                <th>HWID</th>
                <th>Status</th>
                <th>Last Login</th>
                <th>Actions</th>
              </tr>
            </thead>
            <tbody>
              {users.map(u => (
                <tr key={u.id}>
                  <td style={{ fontWeight: 600 }}>{u.username}</td>
                  <td style={{ fontSize: 12, fontFamily: 'monospace' }}>{u.usedKey || '—'}</td>
                  <td>{remainingBadge(u.remainingDays)}</td>
                  <td>
                    <span className={`badge ${u.hasHwid ? 'badge-green' : 'badge-gray'}`}>
                      {u.hasHwid ? 'Bound' : 'None'}
                    </span>
                  </td>
                  <td>
                    <span className={`badge ${isOnline(u.lastVerifiedAt) ? 'badge-green' : 'badge-gray'}`}>
                      {isOnline(u.lastVerifiedAt) ? 'Online' : 'Offline'}
                    </span>
                  </td>
                  <td style={{ fontSize: 12, color: 'var(--text2)' }}>
                    {u.lastLoginAt ? new Date(u.lastLoginAt).toLocaleString() : 'Never'}
                  </td>
                  <td>
                    <div style={{ display: 'flex', gap: 6 }}>
                      <button className="btn btn-ghost" style={{ padding: '4px 10px', fontSize: 12 }} onClick={() => forceLogout(u.id)}>Force Logout</button>
                      <button className="btn btn-ghost" style={{ padding: '4px 10px', fontSize: 12 }} onClick={() => resetHwid(u.id)}>Reset HWID</button>
                      <button className="btn btn-danger" style={{ padding: '4px 10px', fontSize: 12 }} onClick={() => deleteUser(u.id, u.username)}>Delete</button>
                    </div>
                  </td>
                </tr>
              ))}
              {users.length === 0 && <tr><td colSpan={7} style={{ textAlign: 'center', color: 'var(--text2)' }}>No users yet</td></tr>}
            </tbody>
          </table>
        </div>
      )}
    </Layout>
  )
}
