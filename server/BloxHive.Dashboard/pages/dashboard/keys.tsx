import { useEffect, useState, FormEvent } from 'react'
import { useRouter } from 'next/router'
import Layout from '../../components/Layout'

const API = process.env.NEXT_PUBLIC_API_URL || 'http://localhost:5000/api'

interface KeyInfo {
  id: number
  key: string
  durationDays: number | null
  isUsed: boolean
  usedBy: string | null
  createdAt: string
  expiresAt: string | null
}

export default function Keys() {
  const [keys, setKeys] = useState<KeyInfo[]>([])
  const [duration, setDuration] = useState('30')
  const [loading, setLoading] = useState(true)
  const [created, setCreated] = useState('')
  const router = useRouter()

  const token = typeof window !== 'undefined' ? localStorage.getItem('token') : null

  const load = () => {
    fetch(`${API}/admin/keys`, { headers: { Authorization: `Bearer ${token}` } })
      .then(r => { if (!r.ok) { router.push('/'); return }; return r.json() })
      .then(d => { setKeys(d); setLoading(false) })
  }

  useEffect(() => { if (!token) { router.push('/'); return }; load() }, [])

  const createKey = async (e: FormEvent) => {
    e.preventDefault()
    setCreated('')
    const dur = duration === '' ? null : parseInt(duration)
    const res = await fetch(`${API}/admin/keys`, {
      method: 'POST',
      headers: { 'Content-Type': 'application/json', Authorization: `Bearer ${token}` },
      body: JSON.stringify({ durationDays: dur }),
    })
    const data = await res.json()
    setCreated(data.key)
    load()
  }

  const deleteKey = async (id: number) => {
    if (!confirm('Delete this key?')) return
    const res = await fetch(`${API}/admin/keys/${id}`, { method: 'DELETE', headers: { Authorization: `Bearer ${token}` } })
    if (!res.ok) { const d = await res.json(); alert(d.error || 'Failed to delete'); return }
    load()
  }

  const label = (d: number | null) => d === null ? 'Permanent' : `${d} days`

  return (
    <Layout>
      <h1 style={{ fontSize: 22, fontWeight: 700, margin: '0 0 24px 0' }}>License Keys</h1>

      <div className="card" style={{ marginBottom: 24 }}>
        <h2 style={{ fontSize: 14, fontWeight: 600, margin: '0 0 12px 0' }}>Generate Key</h2>
        <form onSubmit={createKey} style={{ display: 'flex', gap: 12, alignItems: 'center' }}>
          <select className="input" value={duration} onChange={e => setDuration(e.target.value)} style={{ width: 180 }}>
            <option value="1">1 Day</option>
            <option value="7">7 Days</option>
            <option value="30">30 Days</option>
            <option value="90">90 Days</option>
            <option value="365">365 Days</option>
            <option value="">Permanent</option>
          </select>
          <button className="btn btn-primary" type="submit">Generate</button>
        </form>
        {created && (
          <div style={{ marginTop: 12, padding: 12, background: 'rgba(99,102,241,0.1)', borderRadius: 8, fontSize: 13 }}>
            <strong>Key created:</strong> <code style={{ fontSize: 15, fontWeight: 600, color: 'var(--accent)' }}>{created}</code>
          </div>
        )}
      </div>

      {loading ? <p style={{ color: 'var(--text2)' }}>Loading...</p> : (
        <div className="card" style={{ padding: 0, overflow: 'hidden' }}>
          <table>
              <thead>
                <tr><th>Key</th><th>Duration</th><th>Status</th><th>Used By</th><th>Created</th><th>Actions</th></tr>
              </thead>
            <tbody>
              {keys.map(k => (
                <tr key={k.id}>
                  <td style={{ fontFamily: 'monospace', fontSize: 12 }}>{k.key}</td>
                  <td>{label(k.durationDays)}</td>
                  <td><span className={`badge ${k.isUsed ? 'badge-gray' : 'badge-green'}`}>{k.isUsed ? 'Used' : 'Available'}</span></td>
                  <td>{k.usedBy || '—'}</td>
                  <td style={{ fontSize: 12, color: 'var(--text2)' }}>{new Date(k.createdAt).toLocaleDateString()}</td>
                  <td>
                    {!k.isUsed && (
                      <button className="btn btn-danger" style={{ padding: '4px 10px', fontSize: 12 }} onClick={() => deleteKey(k.id)}>Delete</button>
                    )}
                  </td>
                </tr>
              ))}
              {keys.length === 0 && <tr><td colSpan={6} style={{ textAlign: 'center', color: 'var(--text2)' }}>No keys yet</td></tr>}
            </tbody>
          </table>
        </div>
      )}
    </Layout>
  )
}
