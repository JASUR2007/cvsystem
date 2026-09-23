import { useState } from 'react'
import { api, json, type Page } from '../shared/api'
import { Pager, Status } from '../shared/ui'
import { useApi } from '../shared/useApi'
import { t } from '../shared/i18n'
import ConfirmModal from '../shared/ConfirmModal'
import '../admin.css'

type User = {
  id: string
  email: string
  firstName: string
  lastName: string
  isBlocked: boolean
  roles: string[]
}

type RecentUser = {
  id: string
  name: string
  role: string
}

type RecentPosition = {
  id: string
  title: string
  level: string | null
  cvCount: number
}

type DashboardData = {
  totalUsers: number
  candidates: number
  recruiters: number
  administrators: number
  blockedUsers: number
  positions: number
  draftCvs: number
  publishedCvs: number
  recentUsers: RecentUser[]
  recentPositions: RecentPosition[]
}

export default function AdminPage() {
  const initialTab = window.location.pathname === '/admin/users' ? 'users' : 'dashboard'
  const [currentTab, setCurrentTab] = useState<'dashboard' | 'users'>(initialTab)

  // Dashboard Stats API
  const stats = useApi<DashboardData>('/admin/dashboard')

  // Users Management State
  const [query, setQuery] = useState('')
  const [roleFilter, setRoleFilter] = useState('')
  const [statusFilter, setStatusFilter] = useState('')
  const [page, setPage] = useState(1)
  const [selected, setSelected] = useState<string[]>([])
  const [message, setMessage] = useState('')
  const [isDeleteModalOpen, setIsDeleteModalOpen] = useState(false)

  const blockedParam = statusFilter === 'blocked' ? '&isBlocked=true' : statusFilter === 'active' ? '&isBlocked=false' : ''
  const roleParam = roleFilter ? `&role=${roleFilter}` : ''

  const users = useApi<Page<User>>(
    `/admin/users?q=${encodeURIComponent(query)}${roleParam}${blockedParam}&page=${page}`
  )

  const chosen = users.data?.items.find(u => u.id === selected[0])

  async function executeDeleteUsers() {
    try {
      await Promise.all(
        selected.map(id => api(`/admin/users/${id}`, { method: 'DELETE' }))
      )
      setSelected([])
      users.reload()
      stats.reload()
      setMessage(t('Users successfully updated (delete).'))
    } catch (cause) {
      setMessage(cause instanceof Error ? cause.message : t('Could not perform operation.'))
    }
  }

  async function action(kind: 'block' | 'unblock' | 'delete') {
    if (kind === 'delete') {
      setIsDeleteModalOpen(true)
      return
    }
    try {
      await Promise.all(
        selected.map(id => api(`/admin/users/${id}/${kind}`, { method: 'POST' }))
      )
      setSelected([])
      users.reload()
      stats.reload()
      setMessage(t(`Users successfully updated (${kind}).`))
    } catch (cause) {
      setMessage(cause instanceof Error ? cause.message : t('Could not perform operation.'))
    }
  }

  async function setRoles(roles: string[]) {
    if (!chosen) return
    try {
      await api(`/admin/users/${chosen.id}/roles`, json('PUT', { roles }))
      users.reload()
      setMessage(t('User roles updated successfully.'))
    } catch (cause) {
      setMessage(cause instanceof Error ? cause.message : t('Could not update roles.'))
    }
  }

  return (
    <div className="admin-layout">
      {/* Sidebar Navigation */}
      <aside className="admin-sidebar">
        <div className="admin-brand-tag">{t('Administration')}</div>
        <nav className="admin-nav-menu">
          <button
            type="button"
            className={`admin-nav-link ${currentTab === 'dashboard' ? 'active' : ''}`}
            onClick={() => setCurrentTab('dashboard')}
          >
            <svg width="18" height="18" viewBox="0 0 24 24" fill="none" stroke="currentColor" strokeWidth="2">
              <rect x="3" y="3" width="7" height="7" />
              <rect x="14" y="3" width="7" height="7" />
              <rect x="14" y="14" width="7" height="7" />
              <rect x="3" y="14" width="7" height="7" />
            </svg>
            <span>{t('Dashboard')}</span>
          </button>

          <button
            type="button"
            className={`admin-nav-link ${currentTab === 'users' ? 'active' : ''}`}
            onClick={() => setCurrentTab('users')}
          >
            <svg width="18" height="18" viewBox="0 0 24 24" fill="none" stroke="currentColor" strokeWidth="2">
              <path d="M17 21v-2a4 4 0 0 0-4-4H5a4 4 0 0 0-4 4v2" />
              <circle cx="9" cy="7" r="4" />
              <path d="M23 21v-2a4 4 0 0 0-3-3.87" />
              <path d="M16 3.13a4 4 0 0 1 0 7.75" />
            </svg>
            <span>{t('Users')}</span>
          </button>

          <a href="/positions" className="admin-nav-link">
            <svg width="18" height="18" viewBox="0 0 24 24" fill="none" stroke="currentColor" strokeWidth="2">
              <rect x="2" y="7" width="20" height="14" rx="2" ry="2" />
              <path d="M16 21V5a2 2 0 0 0-2-2h-4a2 2 0 0 0-2 2v16" />
            </svg>
            <span>{t('Positions')}</span>
          </a>

          <a href="/attributes" className="admin-nav-link">
            <svg width="18" height="18" viewBox="0 0 24 24" fill="none" stroke="currentColor" strokeWidth="2">
              <polygon points="12 2 2 7 12 12 22 7 12 2" />
              <polyline points="2 17 12 22 22 17" />
              <polyline points="2 12 12 17 22 12" />
            </svg>
            <span>{t('Attributes')}</span>
          </a>
        </nav>
      </aside>

      {/* Main Content Area */}
      <main className="admin-content">
        {message && (
          <div className="alert alert-info alert-dismissible fade show" role="alert">
            {message}
            <button type="button" className="btn-close" onClick={() => setMessage('')}></button>
          </div>
        )}

        {currentTab === 'dashboard' && (
          <div>
            <header className="admin-page-header">
              <h1 className="admin-page-title">{t('Admin Dashboard')}</h1>
              <p className="text-muted mb-0">{t('Platform performance, user demographics, and system overview.')}</p>
            </header>

            <Status loading={stats.loading} error={stats.error} />

            {stats.data && (
              <>
                {/* 8 KPI Cards */}
                <div className="admin-kpi-grid">
                  <div className="admin-kpi-card">
                    <span className="admin-kpi-num">{stats.data.totalUsers.toLocaleString()}</span>
                    <span className="admin-kpi-label">{t('Total Users')}</span>
                  </div>
                  <div className="admin-kpi-card">
                    <span className="admin-kpi-num" style={{ color: '#2563EB' }}>
                      {stats.data.candidates.toLocaleString()}
                    </span>
                    <span className="admin-kpi-label">{t('Candidates')}</span>
                  </div>
                  <div className="admin-kpi-card">
                    <span className="admin-kpi-num" style={{ color: '#059669' }}>
                      {stats.data.recruiters.toLocaleString()}
                    </span>
                    <span className="admin-kpi-label">{t('Recruiters')}</span>
                  </div>
                  <div className="admin-kpi-card">
                    <span className="admin-kpi-num" style={{ color: '#7C3AED' }}>
                      {stats.data.administrators.toLocaleString()}
                    </span>
                    <span className="admin-kpi-label">{t('Administrators')}</span>
                  </div>
                  <div className="admin-kpi-card">
                    <span className="admin-kpi-num" style={{ color: '#DC2626' }}>
                      {stats.data.blockedUsers.toLocaleString()}
                    </span>
                    <span className="admin-kpi-label">{t('Blocked Users')}</span>
                  </div>
                  <div className="admin-kpi-card">
                    <span className="admin-kpi-num" style={{ color: '#D97706' }}>
                      {stats.data.positions.toLocaleString()}
                    </span>
                    <span className="admin-kpi-label">{t('Positions')}</span>
                  </div>
                  <div className="admin-kpi-card">
                    <span className="admin-kpi-num" style={{ color: '#4B5563' }}>
                      {stats.data.draftCvs.toLocaleString()}
                    </span>
                    <span className="admin-kpi-label">{t('Draft CVs')}</span>
                  </div>
                  <div className="admin-kpi-card">
                    <span className="admin-kpi-num" style={{ color: '#16A34A' }}>
                      {stats.data.publishedCvs.toLocaleString()}
                    </span>
                    <span className="admin-kpi-label">{t('Published CVs')}</span>
                  </div>
                </div>

                {/* 2-Column Lists: Recent Users & Recent Positions */}
                <div className="admin-two-cols">
                  {/* Recent Users */}
                  <div className="card p-3 border shadow-sm bg-card">
                    <h3 className="h6 mb-3" style={{ fontWeight: 600 }}>{t('Recent Users')}</h3>
                    <div className="table-responsive">
                      <table className="table table-hover align-middle mb-0">
                        <tbody>
                          {stats.data.recentUsers.map(u => (
                            <tr key={u.id}>
                              <td>
                                <a
                                  href={`/profile/${u.id}`}
                                  style={{ fontWeight: 600, color: 'var(--text-primary)', textDecoration: 'none' }}
                                >
                                  {u.name}
                                </a>
                              </td>
                              <td style={{ textAlign: 'right' }}>
                                <span className="badge text-bg-light border">{u.role}</span>
                              </td>
                            </tr>
                          ))}
                        </tbody>
                      </table>
                    </div>
                  </div>

                  {/* Recent Positions */}
                  <div className="card p-3 border shadow-sm bg-card">
                    <h3 className="h6 mb-3" style={{ fontWeight: 600 }}>{t('Recent Positions')}</h3>
                    <div className="table-responsive">
                      <table className="table table-hover align-middle mb-0">
                        <tbody>
                          {stats.data.recentPositions.map(p => (
                            <tr key={p.id}>
                              <td>
                                <a
                                  href={`/positions/${p.id}`}
                                  style={{ fontWeight: 600, color: 'var(--text-primary)', textDecoration: 'none' }}
                                >
                                  {p.title}
                                </a>
                              </td>
                              <td>
                                {p.level && <span className="badge text-bg-secondary">{p.level}</span>}
                              </td>
                              <td style={{ textAlign: 'right' }}>
                                <span className="badge bg-primary-subtle text-primary border border-primary-subtle">
                                  {p.cvCount} {t('CVs')}
                                </span>
                              </td>
                            </tr>
                          ))}
                        </tbody>
                      </table>
                    </div>
                  </div>
                </div>
              </>
            )}
          </div>
        )}

        {currentTab === 'users' && (
          <div>
            <header className="admin-page-header">
              <h1 className="admin-page-title">{t('Users')}</h1>
              <p className="text-muted mb-0">{t('Manage platform users, inspect profiles, configure permissions, and toggle access.')}</p>
            </header>

            {/* Filter toolbar */}
            <div className="admin-users-toolbar">
              <div className="d-flex flex-wrap align-items-center gap-2 flex-grow-1">
                <input
                  className="form-control form-control-sm"
                  style={{ maxWidth: 260 }}
                  value={query}
                  onChange={e => {
                    setQuery(e.target.value)
                    setPage(1)
                  }}
                  placeholder={t('Search users...')}
                />

                <select
                  className="form-select form-select-sm"
                  style={{ width: 'auto' }}
                  value={roleFilter}
                  onChange={e => {
                    setRoleFilter(e.target.value)
                    setPage(1)
                  }}
                >
                  <option value="">{t('All Roles')}</option>
                  <option value="Candidate">{t('Candidates')}</option>
                  <option value="Recruiter">{t('Recruiters')}</option>
                  <option value="Administrator">{t('Administrators')}</option>
                </select>

                <select
                  className="form-select form-select-sm"
                  style={{ width: 'auto' }}
                  value={statusFilter}
                  onChange={e => {
                    setStatusFilter(e.target.value)
                    setPage(1)
                  }}
                >
                  <option value="">{t('All Status')}</option>
                  <option value="active">{t('Active')}</option>
                  <option value="blocked">{t('Blocked')}</option>
                </select>
              </div>

              {/* Toolbar Actions for selected items */}
              <div className="admin-toolbar-actions">
                <button
                  type="button"
                  className="btn btn-admin-block btn-sm"
                  disabled={selected.length === 0}
                  onClick={() => action('block')}
                >
                  {t('Block')}
                </button>
                <button
                  type="button"
                  className="btn btn-outline-success btn-sm"
                  disabled={selected.length === 0}
                  onClick={() => action('unblock')}
                >
                  {t('Unblock')}
                </button>
                <button
                  type="button"
                  className="btn btn-outline-danger btn-sm"
                  disabled={selected.length === 0}
                  onClick={() => action('delete')}
                >
                  {t('Delete')}
                </button>
              </div>
            </div>

            {/* Role Management Panel for selected single user (MOVED TO TOP) */}
            {selected.length === 1 && chosen && (
              <div className="card mb-3 p-3 border bg-secondary-subtle">
                <div className="d-flex align-items-center justify-content-between mb-3">
                  <h3 className="h6 mb-0">
                    {t('Roles for')} {chosen.firstName} {chosen.lastName} ({chosen.email})
                  </h3>
                  <button
                    type="button"
                    className="btn-close"
                    onClick={() => setSelected([])}
                    aria-label="Close"
                  ></button>
                </div>

                <div className="d-flex flex-wrap gap-4">
                  {['Candidate', 'Recruiter', 'Administrator'].map(role => (
                    <label className="form-check-label d-flex align-items-center gap-2" key={role}>
                      <input
                        type="checkbox"
                        className="form-check-input"
                        checked={chosen.roles.includes(role)}
                        onChange={e =>
                          setRoles(
                            e.target.checked
                              ? [...chosen.roles, role]
                              : chosen.roles.filter(item => item !== role)
                          )
                        }
                      />
                      <span>{t(role)}</span>
                    </label>
                  ))}
                </div>
              </div>
            )}

            <Status loading={users.loading} error={users.error} empty={users.data?.items.length === 0} />

            {/* Users Table */}
            {users.data && users.data.items.length > 0 && (
              <div className="card border shadow-sm bg-card overflow-hidden">
                <div className="table-responsive">
                  <table className="table table-hover align-middle mb-0">
                    <thead>
                      <tr>
                        <th style={{ width: 44 }}>
                          <input
                            type="checkbox"
                            className="form-check-input"
                            aria-label="Select all"
                            checked={selected.length === users.data.items.length}
                            onChange={e =>
                              setSelected(e.target.checked ? users.data!.items.map(u => u.id) : [])
                            }
                          />
                        </th>
                        <th>{t('User')}</th>
                        <th>{t('Email')}</th>
                        <th>{t('Roles')}</th>
                        <th>{t('Status')}</th>
                      </tr>
                    </thead>
                    <tbody>
                      {users.data.items.map(user => (
                        <tr
                          key={user.id}
                          className="click-row"
                          onClick={() => setSelected([user.id])}
                        >
                          <td onClick={e => e.stopPropagation()}>
                            <input
                              type="checkbox"
                              className="form-check-input"
                              aria-label={`Select ${user.email}`}
                              checked={selected.includes(user.id)}
                              onChange={e =>
                                setSelected(
                                  e.target.checked
                                    ? [...selected, user.id]
                                    : selected.filter(id => id !== user.id)
                                )
                              }
                            />
                          </td>
                          <td>
                            <a
                              href={`/profile/${user.id}`}
                              style={{ fontWeight: 600, color: 'var(--text-primary)', textDecoration: 'none' }}
                              onClick={e => e.stopPropagation()}
                            >
                              {user.firstName} {user.lastName}
                            </a>
                          </td>
                          <td className="text-muted">{user.email}</td>
                          <td>
                            <div className="d-flex flex-wrap gap-1">
                              {user.roles.map(r => (
                                <span className="badge text-bg-light border" key={r}>
                                  {t(r)}
                                </span>
                              ))}
                            </div>
                          </td>
                          <td>
                            <span
                              className={`badge ${user.isBlocked ? 'text-bg-danger' : 'text-bg-success'}`}
                            >
                              {user.isBlocked ? t('Blocked') : t('Active')}
                            </span>
                          </td>
                        </tr>
                      ))}
                    </tbody>
                  </table>
                </div>
              </div>
            )}

            <div className="mt-3">
              <Pager page={page} total={users.data?.totalPages ?? 0} onPage={setPage} />
            </div>
          </div>
        )}

        <ConfirmModal
          isOpen={isDeleteModalOpen}
          title={t('Delete users')}
          message={`${t('Are you sure you want to delete')} ${selected.length} ${t('selected user(s)?')}`}
          confirmText={t('Delete')}
          cancelText={t('Cancel')}
          confirmVariant="danger"
          onConfirm={executeDeleteUsers}
          onCancel={() => setIsDeleteModalOpen(false)}
        />
      </main>
    </div>
  )
}
