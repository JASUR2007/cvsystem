import { useState } from 'react'
import { api, json, type Page } from '../shared/api'
import { PageTitle, Pager, Status } from '../shared/ui'
import { useApi } from '../shared/useApi'

type User = { id: string; email: string; firstName: string; lastName: string; isBlocked: boolean; roles: string[] }
type Stats = { users: number; blocked: number; positions: number; attributes: number; published: number; drafts: number }

export default function AdminPage() {
  const [query, setQuery] = useState('')
  const [page, setPage] = useState(1)
  const [selected, setSelected] = useState<string[]>([])
  const [message, setMessage] = useState('')
  const stats = useApi<Stats>('/admin/dashboard')
  const users = useApi<Page<User>>(`/admin/users?q=${encodeURIComponent(query)}&page=${page}`)
  const chosen = users.data?.items.find(user => user.id === selected[0])

  async function action(kind: 'block' | 'unblock' | 'delete') {
    if (kind === 'delete' && !window.confirm(`Delete ${selected.length} selected user(s)?`)) return
    try {
      await Promise.all(selected.map(id => api(`/admin/users/${id}${kind === 'delete' ? '' : `/${kind}`}`, { method: kind === 'delete' ? 'DELETE' : 'POST' })))
      setSelected([])
      users.reload()
      stats.reload()
      setMessage('Users updated.')
    } catch (cause) { setMessage(cause instanceof Error ? cause.message : 'Could not update users.') }
  }

  async function setRoles(roles: string[]) {
    if (!chosen) return
    try { await api(`/admin/users/${chosen.id}/roles`, json('PUT', { roles })); users.reload(); setMessage('Roles updated. The user must sign in again.') }
    catch (cause) { setMessage(cause instanceof Error ? cause.message : 'Could not update roles.') }
  }

  return <><section className="surface page-surface"><PageTitle title="Admin dashboard" /><Status loading={stats.loading} error={stats.error} /><div className="stats-grid">{stats.data && Object.entries(stats.data).map(([label, value]) => <div key={label}><strong>{value}</strong><span>{label}</span></div>)}</div></section><section className="surface page-surface"><PageTitle title="Users" /><div className="toolbar"><input className="form-control" value={query} onChange={event => { setQuery(event.target.value); setPage(1) }} placeholder="Search users" /><button className="btn btn-outline-warning" disabled={selected.length === 0} onClick={() => action('block')}>Block</button><button className="btn btn-outline-success" disabled={selected.length === 0} onClick={() => action('unblock')}>Unblock</button><button className="btn btn-outline-danger" disabled={selected.length === 0} onClick={() => action('delete')}>Delete</button></div>{message && <div className="alert alert-info">{message}</div>}<Status loading={users.loading} error={users.error} empty={users.data?.items.length === 0} />{users.data && users.data.items.length > 0 && <div className="table-responsive"><table className="table table-hover"><thead><tr><th><input type="checkbox" aria-label="Select all" checked={selected.length === users.data.items.length} onChange={event => setSelected(event.target.checked ? users.data!.items.map(user => user.id) : [])} /></th><th>User</th><th>Email</th><th>Roles</th><th>Status</th></tr></thead><tbody>{users.data.items.map(user => <tr key={user.id} className="click-row" onClick={() => setSelected([user.id])}><td><input type="checkbox" aria-label={`Select ${user.email}`} checked={selected.includes(user.id)} onChange={event => setSelected(event.target.checked ? [...selected, user.id] : selected.filter(id => id !== user.id))} /></td><td><a href={`/profile/${user.id}`} onClick={event => event.stopPropagation()}>{user.firstName} {user.lastName}</a></td><td>{user.email}</td><td>{user.roles.join(', ')}</td><td><span className={`badge ${user.isBlocked ? 'text-bg-danger' : 'text-bg-success'}`}>{user.isBlocked ? 'Blocked' : 'Active'}</span></td></tr>)}</tbody></table></div>}<Pager page={page} total={users.data?.totalPages ?? 0} onPage={setPage} />{selected.length === 1 && chosen && <div className="edit-panel"><h3>Roles for {chosen.email}</h3><div className="choice-list">{['Candidate','Recruiter','Administrator'].map(role => <label className="check-line" key={role}><input type="checkbox" checked={chosen.roles.includes(role)} onChange={event => setRoles(event.target.checked ? [...chosen.roles, role] : chosen.roles.filter(item => item !== role))} /> {role}</label>)}</div></div>}</section></>
}
