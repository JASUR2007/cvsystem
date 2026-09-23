import { useState } from 'react'
import { api, dateText, type Page, type PositionListItem } from '../shared/api'
import { Pager, Status } from '../shared/ui'
import { useApi } from '../shared/useApi'
import type { CurrentUser } from '../auth/api'
import { t } from '../shared/i18n'
import ConfirmModal from '../shared/ConfirmModal'

export default function PositionsPage({ user }: { user: CurrentUser | null }) {
  const [query, setQuery] = useState('')
  const [level, setLevel] = useState('')
  const [page, setPage] = useState(1)
  const [selected, setSelected] = useState<string[]>([])
  const [message, setMessage] = useState('')
  const [isDeleteModalOpen, setIsDeleteModalOpen] = useState(false)
  const canManage = user?.roles.some(role => role === 'Recruiter' || role === 'Administrator') ?? false
  const result = useApi<Page<PositionListItem>>(`/positions?q=${encodeURIComponent(query)}&level=${level}&page=${page}`)

  async function handleDeleteConfirm() {
    try {
      await Promise.all(selected.map(id => api(`/positions/${id}`, { method: 'DELETE' })))
      setSelected([])
      setMessage(t('Selected positions were deleted.'))
      result.reload()
    } catch (error) {
      setMessage(error instanceof Error ? error.message : t('Could not delete positions.'))
    }
  }

  const hasFilters = query.trim() !== '' || level !== ''

  return (
    <section className="surface page-surface positions-page-surface">
      {/* Page Header */}
      <div className="positions-header-block">
        <div>
          <span className="eyebrow">{t('Positions')}</span>
          <h1 className="page-main-title">{t('Positions')}</h1>
          <p className="page-subtitle">
            {t('Explore open positions and evaluate your CV match with structured requirements.')}
          </p>
        </div>

        {canManage && (
          <a href="/positions/new" className="btn btn-primary create-pos-btn">
            <svg width="18" height="18" viewBox="0 0 24 24" fill="none" stroke="currentColor" strokeWidth="2.5" aria-hidden="true">
              <line x1="12" y1="5" x2="12" y2="19" />
              <line x1="5" y1="12" x2="19" y2="12" />
            </svg>
            <span>{t('Create position')}</span>
          </a>
        )}
      </div>

      {/* Toolbar / Filters */}
      <div className="positions-toolbar">
        <div className="positions-search-wrapper">
          <svg className="toolbar-search-icon" viewBox="0 0 24 24" aria-hidden="true" fill="none" stroke="currentColor" strokeWidth="2">
            <circle cx="11" cy="11" r="7" />
            <path d="m16 16 4 4" />
          </svg>
          <input
            className="form-control toolbar-search-input"
            placeholder={t('Search positions...')}
            value={query}
            onChange={event => { setQuery(event.target.value); setPage(1); }}
          />
          {query && (
            <button
              type="button"
              className="toolbar-clear-btn"
              onClick={() => { setQuery(''); setPage(1); }}
              aria-label="Clear search"
            >
              ×
            </button>
          )}
        </div>

        <select
          className="form-select toolbar-level-select"
          value={level}
          onChange={event => { setLevel(event.target.value); setPage(1); }}
        >
          <option value="">{t('All levels')}</option>
          {['Junior', 'Middle', 'Senior', 'Lead'].map(item => (
            <option key={item} value={item}>{item}</option>
          ))}
        </select>

        {canManage && selected.length === 1 && (
          <div className="d-flex gap-2 ms-auto">
            <button
              type="button"
              className="btn btn-outline-secondary btn-sm d-inline-flex align-items-center gap-1"
              onClick={async () => {
                try {
                  const dup = await api<PositionListItem>(`/positions/${selected[0]}/duplicate`, { method: 'POST' })
                  window.location.assign(`/positions/${dup.id}`)
                } catch (cause) {
                  setMessage(cause instanceof Error ? cause.message : 'Could not duplicate position.')
                }
              }}
            >
              <svg width="15" height="15" viewBox="0 0 24 24" fill="none" stroke="currentColor" strokeWidth="2">
                <rect x="9" y="9" width="13" height="13" rx="2" ry="2" />
                <path d="M5 15H4a2 2 0 0 1-2-2V4a2 2 0 0 1 2-2h9a2 2 0 0 1 2 2v1" />
              </svg>
              <span>{t('Duplicate')}</span>
            </button>
            <a
              href={`/positions/${selected[0]}/edit`}
              className="btn btn-outline-primary btn-sm d-inline-flex align-items-center gap-1"
            >
              <svg width="15" height="15" viewBox="0 0 24 24" fill="none" stroke="currentColor" strokeWidth="2">
                <path d="M11 4H4a2 2 0 0 0-2 2v14a2 2 0 0 0 2 2h14a2 2 0 0 0 2-2v-7" />
                <path d="M18.5 2.5a2.121 2.121 0 0 1 3 3L12 15l-4 1 1-4 9.5-9.5z" />
              </svg>
              <span>{t('Edit')}</span>
            </a>
          </div>
        )}

        {canManage && selected.length > 0 && (
          <button
            className={`btn btn-outline-danger btn-delete-selected ${selected.length !== 1 ? 'ms-auto' : ''}`}
            onClick={() => setIsDeleteModalOpen(true)}
          >
            <svg width="16" height="16" viewBox="0 0 24 24" fill="none" stroke="currentColor" strokeWidth="2">
              <polyline points="3 6 5 6 21 6" />
              <path d="M19 6v14a2 2 0 0 1-2 2H7a2 2 0 0 1-2-2V6m3 0V4a2 2 0 0 1 2-2h4a2 2 0 0 1 2 2v2" />
            </svg>
            <span>{t('Delete selected')} ({selected.length})</span>
          </button>
        )}
      </div>

      {/* Notifications */}
      {message && (
        <div className="alert alert-info alert-dismissible fade show" role="alert">
          {message}
          <button type="button" className="btn-close" onClick={() => setMessage('')} aria-label="Close"></button>
        </div>
      )}

      {/* Status indicator (Loading / Error) */}
      <Status loading={result.loading} error={result.error} />

      {/* Results Content */}
      {result.data && result.data.items.length > 0 ? (
        <div className="positions-table-card">
          <div className="table-responsive">
            <table className="table table-hover align-middle mb-0">
              <thead>
                <tr>
                  {canManage && (
                    <th style={{ width: 44 }}>
                      <input
                        type="checkbox"
                        className="form-check-input"
                        aria-label="Select all"
                        checked={selected.length === result.data.items.length}
                        onChange={event => setSelected(event.target.checked ? result.data!.items.map(item => item.id) : [])}
                      />
                    </th>
                  )}
                  <th>{t('Title')}</th>
                  <th>{t('Company')}</th>
                  <th>{t('Level')}</th>
                  <th>{t('CVs')}</th>
                  <th>{t('Updated')}</th>
                </tr>
              </thead>
              <tbody>
                {result.data.items.map(position => (
                  <tr
                    key={position.id}
                    className="click-row"
                    onClick={() => window.location.assign(`/positions/${position.id}`)}
                  >
                    {canManage && (
                      <td onClick={event => event.stopPropagation()}>
                        <input
                          type="checkbox"
                          className="form-check-input"
                          aria-label={`Select ${position.title}`}
                          checked={selected.includes(position.id)}
                          onChange={event => setSelected(
                            event.target.checked
                              ? [...selected, position.id]
                              : selected.filter(id => id !== position.id)
                          )}
                        />
                      </td>
                    )}
                    <td>
                      <a className="position-title-link" href={`/positions/${position.id}`}>
                        {position.title}
                      </a>
                    </td>
                    <td>{position.company || '—'}</td>
                    <td>
                      <span className="level-pill">{position.level || '—'}</span>
                    </td>
                    <td>
                      <span className="cv-count-badge">{position.cvCount}</span>
                    </td>
                    <td>{dateText(position.updatedAt)}</td>
                  </tr>
                ))}
              </tbody>
            </table>
          </div>
        </div>
      ) : (
        !result.loading && (
          <div className="empty-positions-state">
            <div className="empty-icon-box">
              <svg viewBox="0 0 24 24" fill="none" stroke="currentColor" strokeWidth="1.5" aria-hidden="true">
                <rect x="2" y="7" width="20" height="14" rx="2" ry="2" />
                <path d="M16 21V5a2 2 0 0 0-2-2h-4a2 2 0 0 0-2 2v16" />
              </svg>
            </div>
            <h3>{t('No records found.')}</h3>
            <p>
              {hasFilters
                ? t('No positions match your current search criteria. Try adjusting or clearing your filters.')
                : t('No positions have been posted yet.')}
            </p>
            {hasFilters && (
              <button
                type="button"
                className="btn btn-outline-primary btn-sm"
                onClick={() => { setQuery(''); setLevel(''); setPage(1); }}
              >
                {t('Reset filters')}
              </button>
            )}
          </div>
        )
      )}

      {/* Pagination */}
      <Pager page={page} total={result.data?.totalPages ?? 0} onPage={setPage} />

      <ConfirmModal
        isOpen={isDeleteModalOpen}
        title="Delete positions"
        message={`${t('Are you sure you want to delete')} ${selected.length} ${t('selected position(s)?')}`}
        confirmText="Delete"
        cancelText="Cancel"
        confirmVariant="danger"
        onConfirm={handleDeleteConfirm}
        onCancel={() => setIsDeleteModalOpen(false)}
      />
    </section>
  )
}
