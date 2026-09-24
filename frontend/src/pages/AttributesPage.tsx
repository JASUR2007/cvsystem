import { useState, type FormEvent } from 'react'
import { api, json, type AttributeListItem, type AttributeType, type Page } from '../shared/api'
import { PageTitle, Pager, Status } from '../shared/ui'
import { useApi } from '../shared/useApi'
import { t } from '../shared/i18n'
import ConfirmModal from '../shared/ConfirmModal'

const DEFAULT_CATEGORIES = [
  'Technical Skills',
  'Soft Skills',
  'Certificates',
  'Language',
  'Education',
  'Domain Knowledge',
  'Personal Information',
  'General',
]

const ATTRIBUTE_TYPES: AttributeType[] = [
  'String',
  'Text',
  'Image',
  'Numeric',
  'Date',
  'Period',
  'Boolean',
  'Dropdown',
]

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

  // In-page creation modal state
  const [isCreateModalOpen, setIsCreateModalOpen] = useState(false)
  const [newAttrName, setNewAttrName] = useState('')
  const [newAttrDesc, setNewAttrDesc] = useState('')
  const [newAttrCategory, setNewAttrCategory] = useState('Technical Skills')
  const [newAttrType, setNewAttrType] = useState<AttributeType>('String')
  const [newAttrOptions, setNewAttrOptions] = useState<string[]>([''])
  const [creatingAttr, setCreatingAttr] = useState(false)
  const [createAttrError, setCreateAttrError] = useState('')

  const categories = useApi<string[]>('/attribute-categories')
  const result = useApi<Page<AttributeListItem>>(
    `/attributes?prefix=${encodeURIComponent(prefix)}&category=${encodeURIComponent(category)}&type=${type}&recent=${recent}&page=${page}`
  )

  async function handleDeleteConfirm() {
    setMessage('')
    setErrorMessage('')
    const toDelete = [...selected]
    if (result.data) {
      result.setData({
        ...result.data,
        items: result.data.items.filter(item => !toDelete.includes(item.id)),
        totalItems: Math.max(0, result.data.totalItems - toDelete.length),
      })
    }
    setSelected([])
    setIsDeleteModalOpen(false)

    try {
      await Promise.all(toDelete.map(id => api(`/attributes/${id}`, { method: 'DELETE' })))
      result.reload()
      setMessage(t('Attributes deleted.'))
    } catch (cause) {
      const err = cause instanceof Error ? cause.message : String(cause)
      if (err.includes('in use') || err.includes('409')) {
        setErrorMessage(t('Cannot delete: one or more attributes are in use by positions or candidate profiles.'))
      } else {
        setErrorMessage(err || t('Could not delete attributes.'))
      }
      result.reload()
    }
  }

  async function handleCreateAttribute(e: FormEvent) {
    e.preventDefault()
    if (!newAttrName.trim()) return
    setCreatingAttr(true)
    setCreateAttrError('')

    try {
      const created = await api<AttributeListItem>(
        '/attributes',
        json('POST', {
          name: newAttrName.trim(),
          description: newAttrDesc.trim(),
          category: newAttrCategory,
          type: newAttrType,
          options: newAttrType === 'Dropdown' ? newAttrOptions.map(o => o.trim()).filter(Boolean) : [],
          version: 1,
        })
      )

      if (result.data) {
        result.setData({
          ...result.data,
          items: [created, ...result.data.items],
          totalItems: result.data.totalItems + 1,
        })
      }

      setIsCreateModalOpen(false)
      setNewAttrName('')
      setNewAttrDesc('')
      setNewAttrCategory('Technical Skills')
      setNewAttrType('String')
      setNewAttrOptions([''])
      setMessage(t('Attribute added.'))
      result.reload()
      categories.reload()
    } catch (cause) {
      setCreateAttrError(cause instanceof Error ? cause.message : t('Could not save attribute.'))
    } finally {
      setCreatingAttr(false)
    }
  }

  const categoryOptions =
    categories.data && categories.data.length > 0
      ? Array.from(new Set([...categories.data, ...DEFAULT_CATEGORIES]))
      : DEFAULT_CATEGORIES

  return (
    <section className="surface page-surface">
      <PageTitle
        title={t('Attribute library')}
        action={
          <button
            type="button"
            className="btn btn-primary btn-sm fw-bold px-3 d-inline-flex align-items-center justify-content-center"
            style={{ fontSize: '1.25rem', lineHeight: 1, minWidth: '40px' }}
            title={t('New attribute')}
            onClick={() => setIsCreateModalOpen(true)}
          >
            +
          </button>
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
            <option key={item} value={item}>{t(item)}</option>
          ))}
        </select>
        <select
          className="form-select"
          style={{ maxWidth: 160 }}
          value={type}
          onChange={event => { setType(event.target.value); setPage(1) }}
        >
          <option value="">{t('All types')}</option>
          {ATTRIBUTE_TYPES.map(item => (
            <option key={item} value={item}>{t(item)}</option>
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
        {selected.length === 1 && (
          <a
            href={`/attributes/${selected[0]}/edit`}
            className="btn btn-outline-primary btn-sm d-inline-flex align-items-center gap-1 ms-auto"
            title={t('Edit')}
          >
            <svg width="15" height="15" viewBox="0 0 24 24" fill="none" stroke="currentColor" strokeWidth="2">
              <path d="M11 4H4a2 2 0 0 0-2 2v14a2 2 0 0 0 2 2h14a2 2 0 0 0 2-2v-7" />
              <path d="M18.5 2.5a2.121 2.121 0 0 1 3 3L12 15l-4 1 1-4 9.5-9.5z" />
            </svg>
            <span className="btn-responsive-text">{t('Edit')}</span>
          </a>
        )}
        <button
          className={`btn btn-outline-danger btn-sm d-inline-flex align-items-center gap-1 ${selected.length !== 1 ? 'ms-auto' : ''}`}
          disabled={selected.length === 0}
          title={t('Delete selected')}
          onClick={() => setIsDeleteModalOpen(true)}
        >
          <svg width="15" height="15" viewBox="0 0 24 24" fill="none" stroke="currentColor" strokeWidth="2">
            <polyline points="3 6 5 6 21 6" />
            <path d="M19 6v14a2 2 0 0 1-2 2H7a2 2 0 0 1-2-2V6m3 0V4a2 2 0 0 1 2-2h4a2 2 0 0 1 2 2v2" />
          </svg>
          <span className="btn-responsive-text">{t('Delete selected')} {selected.length > 0 ? `(${selected.length})` : ''}</span>
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
                  <td className="attribute-category-text">{t(item.category)}</td>
                  <td>
                    <span className="badge text-bg-light border font-monospace">{t(item.type)}</span>
                  </td>
                  <td>
                    <span
                      className={`badge ${item.usageCount > 0 ? 'bg-primary text-white' : 'text-bg-light border'}`}
                      style={item.usageCount > 0 ? { color: '#ffffff', backgroundColor: '#2563eb' } : undefined}
                    >
                      {item.usageCount}
                    </span>
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

      {/* In-page Create Attribute Modal */}
      {isCreateModalOpen && (
        <div
          className="modal fade show d-block"
          tabIndex={-1}
          role="dialog"
          aria-modal="true"
          style={{ backgroundColor: 'rgba(0, 0, 0, 0.55)', zIndex: 1055 }}
          onClick={() => setIsCreateModalOpen(false)}
        >
          <div
            className="modal-dialog modal-dialog-centered"
            role="document"
            style={{ maxWidth: 540 }}
            onClick={e => e.stopPropagation()}
          >
            <form onSubmit={handleCreateAttribute} className="modal-content shadow-lg border-0">
              <div className="modal-header border-bottom">
                <h5 className="modal-title h6 mb-0 fw-bold">{t('Create attribute')}</h5>
                <button
                  type="button"
                  className="btn-close"
                  onClick={() => setIsCreateModalOpen(false)}
                  aria-label="Close"
                />
              </div>

              <div className="modal-body py-3">
                {createAttrError && (
                  <div className="alert alert-danger py-2 mb-3" role="alert">
                    {createAttrError}
                  </div>
                )}

                <div className="mb-3">
                  <label className="form-label fw-semibold">{t('Name')} *</label>
                  <input
                    className="form-control"
                    value={newAttrName}
                    onChange={e => setNewAttrName(e.target.value)}
                    placeholder={t('e.g. Docker, English Level, CAP')}
                    required
                    maxLength={160}
                    autoFocus
                  />
                </div>

                <div className="row g-2 mb-3">
                  <div className="col-sm-6">
                    <label className="form-label fw-semibold">{t('Category')}</label>
                    <select
                      className="form-select"
                      value={newAttrCategory}
                      onChange={e => setNewAttrCategory(e.target.value)}
                    >
                      {categoryOptions.map(cat => (
                        <option key={cat} value={cat}>
                          {t(cat)}
                        </option>
                      ))}
                    </select>
                  </div>

                  <div className="col-sm-6">
                    <label className="form-label fw-semibold">{t('Type')}</label>
                    <select
                      className="form-select"
                      value={newAttrType}
                      onChange={e => setNewAttrType(e.target.value as AttributeType)}
                    >
                      {ATTRIBUTE_TYPES.map(tp => (
                        <option key={tp} value={tp}>
                          {t(tp)}
                        </option>
                      ))}
                    </select>
                  </div>
                </div>

                {newAttrType === 'Dropdown' && (
                  <div className="border rounded p-3 bg-light-subtle mb-3">
                    <h6 className="fw-bold mb-2">{t('Dropdown Options')}</h6>
                    <div className="d-grid gap-2 mb-2">
                      {newAttrOptions.map((opt, idx) => (
                        <div className="d-flex gap-2" key={idx}>
                          <input
                            className="form-control form-control-sm"
                            value={opt}
                            onChange={e =>
                              setNewAttrOptions(
                                newAttrOptions.map((o, p) => (p === idx ? e.target.value : o))
                              )
                            }
                            placeholder={`${t('Option')} ${idx + 1}`}
                            required
                          />
                          <button
                            type="button"
                            className="btn btn-outline-danger btn-sm"
                            disabled={newAttrOptions.length <= 1}
                            onClick={() => setNewAttrOptions(newAttrOptions.filter((_, p) => p !== idx))}
                          >
                            ×
                          </button>
                        </div>
                      ))}
                    </div>
                    <button
                      type="button"
                      className="btn btn-outline-primary btn-sm"
                      onClick={() => setNewAttrOptions([...newAttrOptions, ''])}
                    >
                      + {t('Add option')}
                    </button>
                  </div>
                )}

                <div className="mb-2">
                  <label className="form-label fw-semibold">{t('Description')}</label>
                  <textarea
                    className="form-control"
                    rows={2}
                    value={newAttrDesc}
                    onChange={e => setNewAttrDesc(e.target.value)}
                    placeholder={t('Provide guidance on how to evaluate or fill this attribute...')}
                    maxLength={2000}
                  />
                </div>
              </div>

              <div className="modal-footer border-top">
                <button
                  type="button"
                  className="btn btn-outline-secondary btn-sm"
                  onClick={() => setIsCreateModalOpen(false)}
                >
                  {t('Cancel')}
                </button>
                <button
                  type="submit"
                  className="btn btn-primary btn-sm"
                  disabled={creatingAttr}
                >
                  {creatingAttr ? t('Saving...') : t('Save attribute')}
                </button>
              </div>
            </form>
          </div>
        </div>
      )}
    </section>
  )
}
