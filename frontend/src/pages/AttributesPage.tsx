import { useState } from 'react'
import { api, type AttributeListItem, type Page } from '../shared/api'
import { PageTitle, Pager, Status } from '../shared/ui'
import { useApi } from '../shared/useApi'
import { t } from '../shared/i18n'
import ConfirmModal from '../shared/ConfirmModal'

export default function AttributesPage() {
  const [prefix, setPrefix] = useState('')
  const [category, setCategory] = useState('')
  const [type, setType] = useState('')
  const [recent, setRecent] = useState(false)
  const [page, setPage] = useState(1)
  const [selected, setSelected] = useState<string[]>([])
  const [message, setMessage] = useState('')
  const [errorMessage, setErrorMessage] = useState('')
  const [isDeleteModalOpen, setIsDeleteModalOpen] = useState(false)

  const categories = useApi<string[]>('/attribute-categories')
  const result = useApi<Page<AttributeListItem>>(
    `/attributes?prefix=${encodeURIComponent(prefix)}&category=${encodeURIComponent(category)}&type=${type}&recent=${recent}&page=${page}`
  )

  async function handleDeleteConfirm() {
    setMessage('')
    setErrorMessage('')
    try {
      await Promise.all(selected.map(id => api(`/attributes/${id}`, { method: 'DELETE' })))
      setSelected([])
      result.reload()
      setMessage(t('Attributes deleted.'))
    } catch (cause) {
      const err = cause instanceof Error ? cause.message : String(cause)
      if (err.includes('in use') || err.includes('409')) {
        setErrorMessage(t('Cannot delete: one or more attributes are in use by positions or candidate profiles.'))
      } else {
        setErrorMessage(err || t('Could not delete attributes.'))
      }
    }
  }

  const categoryOptions = categories.data && categories.data.length > 0
    ? categories.data
    : ['Technical Skills', 'Soft Skills', 'Certificates', 'Language', 'Education', 'Domain Knowledge', 'Personal Information', 'General']

  return (
    <section className="surface page-surface">
      <PageTitle
        title="Attribute library"
        action={
          <a href="/attributes/new" className="btn btn-primary btn-sm">
            + {t('New attribute')}
          </a>
        }
      />

      <div className="toolbar d-flex flex-wrap gap-2 mb-3">
        <input
          className="form-control"
          style={{ maxWidth: 220 }}
          value={prefix}
          onChange={event => { setPrefix(event.target.value); setPage(1) }}
          placeholder={t('Search by prefix')}
        />
        <select
          className="form-select"
          style={{ maxWidth: 180 }}
          value={category}
          onChange={event => { setCategory(event.target.value); setPage(1) }}
        >
          <option value="">{t('All categories')}</option>
          {categoryOptions.map(item => (
            <option key={item} value={item}>{item}</option>
          ))}
        </select>
        <select
          className="form-select"
          style={{ maxWidth: 160 }}
          value={type}
          onChange={event => { setType(event.target.value); setPage(1) }}
        >
          <option value="">{t('All types')}</option>
          {['String', 'Text', 'Image', 'Numeric', 'Date', 'Period', 'Boolean', 'Dropdown'].map(item => (
            <option key={item} value={item}>{item}</option>
          ))}
        </select>
        <label className="check-line d-inline-flex align-items-center gap-1 ms-2" style={{ userSelect: 'none' }}>
          <input
            type="checkbox"
            className="form-check-input mt-0"
            checked={recent}
            onChange={event => setRecent(event.target.checked)}
          />{' '}
          {t('Recent')}
        </label>
        <button
          className="btn btn-outline-danger btn-sm ms-auto d-inline-flex align-items-center gap-1"
          disabled={selected.length === 0}
          onClick={() => setIsDeleteModalOpen(true)}
        >
          <svg width="15" height="15" viewBox="0 0 24 24" fill="none" stroke="currentColor" strokeWidth="2">
            <polyline points="3 6 5 6 21 6" />
            <path d="M19 6v14a2 2 0 0 1-2 2H7a2 2 0 0 1-2-2V6m3 0V4a2 2 0 0 1 2-2h4a2 2 0 0 1 2 2v2" />
          </svg>
          <span>{t('Delete selected')} {selected.length > 0 ? `(${selected.length})` : ''}</span>
        </button>
      </div>

      {message && (
        <div className="alert alert-success alert-dismissible fade show" role="alert">
          {message}
          <button type="button" className="btn-close" onClick={() => setMessage('')} aria-label="Close" />
        </div>
      )}

      {errorMessage && (
        <div className="alert alert-danger alert-dismissible fade show" role="alert">
          {errorMessage}
          <button type="button" className="btn-close" onClick={() => setErrorMessage('')} aria-label="Close" />
        </div>
      )}

      <Status loading={result.loading} error={result.error} empty={result.data?.items.length === 0} />

      {result.data && result.data.items.length > 0 && (
        <div className="table-responsive">
          <table className="table table-hover align-middle mb-0">
            <thead>
              <tr>
                <th style={{ width: 44 }}>
                  <input
                    type="checkbox"
                    className="form-check-input"
                    aria-label="Select all"
                    checked={
                      selected.length > 0 &&
                      selected.length === result.data.items.filter(item => !item.isBuiltIn).length
                    }
                    onChange={event =>
                      setSelected(
                        event.target.checked
                          ? result.data!.items.filter(item => !item.isBuiltIn).map(item => item.id)
                          : []
                      )
                    }
                  />
                </th>
                <th>{t('Name')}</th>
                <th>{t('Category')}</th>
                <th>{t('Type')}</th>
                <th>{t('Usage')}</th>
                <th style={{ textAlign: 'right' }}>{t('Actions')}</th>
              </tr>
            </thead>
            <tbody>
              {result.data.items.map(item => (
                <tr
                  key={item.id}
                  className="click-row"
                  onClick={() => window.location.assign(`/attributes/${item.id}/edit`)}
                >
                  <td onClick={event => event.stopPropagation()}>
                    {!item.isBuiltIn ? (
                      <input
                        type="checkbox"
                        className="form-check-input"
                        aria-label={`Select ${item.name}`}
                        checked={selected.includes(item.id)}
                        onChange={event =>
                          setSelected(
                            event.target.checked
                              ? [...selected, item.id]
                              : selected.filter(id => id !== item.id)
                          )
                        }
                      />
                    ) : null}
                  </td>
                  <td>
                    <a
                      href={`/attributes/${item.id}/edit`}
                      className="fw-bold text-decoration-none"
                      style={{ color: 'var(--text-primary)' }}
                    >
                      {item.name}
                    </a>
                    {item.isBuiltIn && (
                      <span className="badge text-bg-secondary ms-2">{t('Built-in')}</span>
                    )}
                  </td>
                  <td>{item.category}</td>
                  <td>
                    <span className="badge text-bg-light border font-monospace">{item.type}</span>
                  </td>
                  <td>
                    <span className={`badge ${item.usageCount > 0 ? 'text-bg-info' : 'text-bg-light border'}`}>
                      {item.usageCount}
                    </span>
                  </td>
                  <td style={{ textAlign: 'right' }} onClick={event => event.stopPropagation()}>
                    {!item.isBuiltIn ? (
                      <div className="d-inline-flex gap-1 justify-content-end">
                        <a
                          href={`/attributes/${item.id}/edit`}
                          className="btn btn-outline-primary btn-sm py-1 px-2 d-inline-flex align-items-center gap-1"
                          title={t('Edit')}
                        >
                          <svg width="14" height="14" viewBox="0 0 24 24" fill="none" stroke="currentColor" strokeWidth="2">
                            <path d="M11 4H4a2 2 0 0 0-2 2v14a2 2 0 0 0 2 2h14a2 2 0 0 0 2-2v-7" />
                            <path d="M18.5 2.5a2.121 2.121 0 0 1 3 3L12 15l-4 1 1-4 9.5-9.5z" />
                          </svg>
                          <span className="attr-btn-label">{t('Edit')}</span>
                        </a>
                        <button
                          type="button"
                          className="btn btn-outline-danger btn-sm py-1 px-2 d-inline-flex align-items-center gap-1"
                          title={t('Delete')}
                          onClick={() => {
                            setSelected([item.id])
                            setIsDeleteModalOpen(true)
                          }}
                        >
                          <svg width="14" height="14" viewBox="0 0 24 24" fill="none" stroke="currentColor" strokeWidth="2">
                            <polyline points="3 6 5 6 21 6" />
                            <path d="M19 6v14a2 2 0 0 1-2 2H7a2 2 0 0 1-2-2V6m3 0V4a2 2 0 0 1 2-2h4a2 2 0 0 1 2 2v2" />
                          </svg>
                          <span className="attr-btn-label">{t('Delete')}</span>
                        </button>
                      </div>
                    ) : (
                      <span className="text-muted small">—</span>
                    )}
                  </td>
                </tr>
              ))}
            </tbody>
          </table>
        </div>
      )}

      <Pager page={page} total={result.data?.totalPages ?? 0} onPage={setPage} />

      <ConfirmModal
        isOpen={isDeleteModalOpen}
        title={t('Delete')}
        message={`${t('Are you sure you want to delete')} ${selected.length} ${t('selected attribute(s)?')}`}
        confirmText={t('Delete')}
        cancelText={t('Cancel')}
        confirmVariant="danger"
        onConfirm={handleDeleteConfirm}
        onCancel={() => setIsDeleteModalOpen(false)}
      />
    </section>
  )
}
