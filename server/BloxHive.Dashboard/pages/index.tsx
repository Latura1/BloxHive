import { useState, FormEvent } from 'react'
import { useRouter } from 'next/router'

const API = process.env.NEXT_PUBLIC_API_URL || 'http://localhost:5000/api'

export default function Login() {
  const [password, setPassword] = useState('')
  const [error, setError] = useState('')
  const [loading, setLoading] = useState(false)
  const router = useRouter()

  const handleSubmit = async (e: FormEvent) => {
    e.preventDefault()
    setLoading(true)
    setError('')

    try {
      const ctrl = new AbortController()
      const timeout = setTimeout(() => ctrl.abort(), 10000)
      const res = await fetch(`${API}/admin/login`, {
        method: 'POST',
        headers: { 'Content-Type': 'application/json' },
        body: JSON.stringify({ password }),
        signal: ctrl.signal,
      })
      clearTimeout(timeout)

      if (!res.ok) {
        setError('Wrong password')
        setLoading(false)
        return
      }

      const data = await res.json()
      localStorage.setItem('token', data.token)
      router.push('/dashboard')
    } catch {
      setError('Connection error - server unreachable')
      setLoading(false)
    }
  }

  return (
    <div style={{ display: 'flex', alignItems: 'center', justifyContent: 'center', minHeight: '100vh' }}>
      <div className="card" style={{ width: 360 }}>
        <div style={{ textAlign: 'center', marginBottom: 24 }}>
          <div style={{ fontSize: 28, fontWeight: 700 }}>BloxHive</div>
          <div style={{ color: 'var(--text2)', fontSize: 13, marginTop: 4 }}>Admin Dashboard</div>
        </div>
        <form onSubmit={handleSubmit}>
          <input
            className="input"
            type="password"
            placeholder="Admin Password"
            value={password}
            onChange={e => setPassword(e.target.value)}
            autoFocus
            style={{ width: '100%', boxSizing: 'border-box' }}
          />
          {error && <p style={{ color: 'var(--danger)', fontSize: 12, marginTop: 8 }}>{error}</p>}
          <button className="btn btn-primary" type="submit" disabled={loading} style={{ width: '100%', marginTop: 16 }}>
            {loading ? 'Signing in...' : 'Sign In'}
          </button>
        </form>
      </div>
    </div>
  )
}
