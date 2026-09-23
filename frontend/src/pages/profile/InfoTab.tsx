import { useState } from 'react'
import { api, json, type AttributeDetail, type AttributeListItem, type AttributeValue, type Page } from '../../shared/api'
import AttributeEditor from '../../shared/AttributeEditor'
import { Status } from '../../shared/ui'
import { useApi } from '../../shared/useApi'
import { t } from '../../shared/i18n'

import ConfirmModal from '../../shared/ConfirmModal'

export default function InfoTab({ userId }: { userId?: string }) {
  const target = userId ? `?userId=${userId}` : ''
  const result = useApi<AttributeValue[]>(`/profile/attributes${target}`)
  const [lookup, setLookup] = useState('')
  const library = useApi<Page<AttributeListItem>>(`/attributes?prefix=${encodeURIComponent(lookup)}&pageSize=20`)
  const [selected, setSelected] = useState<string[]>([])
  const [editing, setEditing] = useState<AttributeValue | null>(null)
  const definition = useApi<AttributeDetail>(editing ? `/attributes/${editing.attributeId}` : null)
  const [message, setMessage] = useState('')
  const [isDeleteModalOpen, setIsDeleteModalOpen] = useState(false)

  async function add(id: string) {
    try {
      await api(`/profile/attributes/${id}${target}`, { method: 'POST' })
      result.reload()
      setMessage(t('Attribute added.'))
    } catch (cause) {
      setMessage(cause instanceof Error ? cause.message : t('Could not add attribute.'))
    }
  }

  async function save() {
    if (!editing) return
    try {
      await api(`/profile/attributes/${editing.attributeId}${target}`, json('PUT', { version: editing.version, value: editing }))
      setEditing(null)
      result.reload()
      setMessage(t('Value saved.'))
    } catch (cause) {
      setMessage(cause instanceof Error ? cause.message : t('Could not save value.'))
    }
  }

  async function handleRemoveConfirm() {
    try {
      await Promise.all(selected.map(id => api(`/profile/attributes/${id}${target}`, { method: 'DELETE' })))
      setSelected([])
      result.reload()
      setMessage(t('Attributes removed.'))
    } catch (cause) {
      setMessage(cause instanceof Error ? cause.message : t('Could not remove attributes.'))
    }
  }

  return (
    <div className="profile-settings-card">
      <div className="profile-card-header">
        <div>
          <h2 className="profile-card-title">{t('Professional information')}</h2>
          <p className="text-muted mb-0" style={{ fontSize: '0.875rem' }}>
            {t('Select an attribute to edit its value in your profile.')}
          </p>
        </div>

        <div className="d-flex gap-2">
          {selected.length === 1 && (
            <button
              type="button"
              className="btn btn-outline-primary btn-sm d-inline-flex align-items-center gap-1"
              title={t('Edit')}
              onClick={() => {
                const val = result.data?.find(v => v.attributeId === selected[0])
                if (val) setEditing(val)
              }}
            >
              <svg width="14" height="14" viewBox="0 0 24 24" fill="none" stroke="currentColor" strokeWidth="2">
                <path d="M11 4H4a2 2 0 0 0-2 2v14a2 2 0 0 0 2 2h14a2 2 0 0 0 2-2v-7" />
                <path d="M18.5 2.5a2.121 2.121 0 0 1 3 3L12 15l-4 1 1-4 9.5-9.5z" />
              </svg>
              <span className="btn-responsive-text">{t('Edit')}</span>
            </button>
          )}
          {selected.length > 0 && (
            <button
              className="btn btn-outline-danger btn-sm d-inline-flex align-items-center gap-1"
              title={t('Delete')}
              onClick={() => setIsDeleteModalOpen(true)}
            >
              <svg width="14" height="14" viewBox="0 0 24 24" fill="none" stroke="currentColor" strokeWidth="2">
                <polyline points="3 6 5 6 21 6" />
                <path d="M19 6v14a2 2 0 0 1-2 2H7a2 2 0 0 1-2-2V6m3 0V4a2 2 0 0 1 2-2h4a2 2 0 0 1 2 2v2" />
              </svg>
              <span className="btn-responsive-text">{t('Delete')} ({selected.length})</span>
            </button>
          )}
        </div>
      </div>

      {/* Attribute quick adder */}
      <div className="mb-4">
        <label className="form-label" style={{ fontWeight: 600, fontSize: '0.875rem' }}>
          {t('Add Attribute')}
        </label>
        <div className="d-flex gap-2 mb-2">
          <input
            className="form-control form-control-sm"
            placeholder={t('Search by prefix')}
            value={lookup}
            onChange={e => setLookup(e.target.value)}
          />
        </div>
        <div className="d-flex flex-wrap gap-1">
          {library.data?.items
            .filter(item => !item.isBuiltIn && !result.data?.some(val => val.attributeId === item.id))
            .slice(0, 10)
            .map(item => (
              <button
                type="button"
                className="btn btn-sm btn-outline-secondary"
                key={item.id}
                onClick={() => add(item.id)}
                style={{ fontSize: '0.75rem', padding: '0.2rem 0.6rem' }}
              >
                + {item.name} <small className="text-muted">({item.category})</small>
              </button>
            ))}
        </div>
      </div>

      {message && (
        <div className="alert alert-info alert-dismissible fade show" role="alert">
          {message}
          <button type="button" className="btn-close" onClick={() => setMessage('')}></button>
        </div>
      )}

      <Status loading={result.loading} error={result.error} empty={result.data?.length === 0} />

      {result.data && (
        <div className="table-responsive">
          <table className="table table-hover align-middle mb-0">
            <thead>
              <tr>
                <th style={{ width: 40 }}>
                  <input
                    type="checkbox"
                    className="form-check-input"
                    aria-label="Select all"
                    checked={
                      selected.length > 0 &&
                      selected.length ===
                        result.data.filter(v => !['First Name', 'Last Name', 'Location', 'Personal Photo'].includes(v.name)).length
                    }
                    onChange={e =>
                      setSelected(
                        e.target.checked
                          ? result.data!
                              .filter(v => !['First Name', 'Last Name', 'Location', 'Personal Photo'].includes(v.name))
                              .map(v => v.attributeId)
                          : []
                      )
                    }
                  />
                </th>
                <th>{t('Attribute')}</th>
                <th>{t('Value')}</th>
              </tr>
            </thead>
            <tbody>
              {result.data.map(value => {
                const isBuiltIn = ['First Name', 'Last Name', 'Location', 'Personal Photo'].includes(value.name)
                const valStr =
                  value.textValue ??
                  value.numberValue?.toString() ??
                  value.dateValue ??
                  value.booleanValue?.toString() ??
                  (value.selectedOptionId ? t('Selected') : value.imageObjectKey ? t('Image') : null)

                return (
                  <tr
                    key={value.attributeId}
                    className="click-row"
                    onClick={() => setEditing(value)}
                  >
                    <td onClick={e => e.stopPropagation()}>
                      {!isBuiltIn && (
                        <input
                          type="checkbox"
                          className="form-check-input"
                          checked={selected.includes(value.attributeId)}
                          onChange={e =>
                            setSelected(
                              e.target.checked
                                ? [...selected, value.attributeId]
                                : selected.filter(id => id !== value.attributeId)
                            )
                          }
                          aria-label={`Select ${value.name}`}
                        />
                      )}
                    </td>
                    <td>
                      <strong>{value.name}</strong>
                      <small className="text-muted d-block">{value.category}</small>
                    </td>
                    <td>
                      {valStr ? (
                        <span>{valStr}</span>
                      ) : (
                        <span className="val-empty">{t('(empty)')}</span>
                      )}
                    </td>
                  </tr>
                )
              })}
            </tbody>
          </table>
        </div>
      )}

      {/* Edit Attribute Modal */}
      {editing && (
        <div
          className="modal fade show d-block"
          tabIndex={-1}
          role="dialog"
          aria-modal="true"
          style={{ backgroundColor: 'rgba(0, 0, 0, 0.5)', zIndex: 1050 }}
          onClick={() => setEditing(null)}
        >
          <div
            className="modal-dialog modal-dialog-centered"
            style={{ maxWidth: 540 }}
            onClick={e => e.stopPropagation()}
          >
            <div className="modal-content shadow-lg border-0">
              <div className="modal-header border-bottom">
                <h3 className="modal-title h5 mb-0" style={{ fontWeight: 600 }}>
                  {t('Edit')} {editing.name}
                </h3>
                <button
                  type="button"
                  className="btn-close"
                  onClick={() => setEditing(null)}
                  aria-label="Close"
                ></button>
              </div>

              <div className="modal-body py-3">
                {['First Name', 'Last Name', 'Location', 'Personal Photo'].includes(editing.name) ? (
                  <p className="text-muted mb-0">{t('Edit this built-in field on the Me tab.')}</p>
                ) : (
                  <>
                    <Status loading={definition.loading} error={definition.error} />
                    <div className="mb-2">
                      <AttributeEditor
                        value={editing}
                        options={definition.data?.options}
                        onChange={setEditing}
                        userId={userId}
                      />
                    </div>
                  </>
                )}
              </div>

              <div className="modal-footer border-top">
                <button
                  type="button"
                  className="btn btn-outline-secondary btn-sm"
                  onClick={() => setEditing(null)}
                >
                  {t('Cancel')}
                </button>
                {!['First Name', 'Last Name', 'Location', 'Personal Photo'].includes(editing.name) && (
                  <button type="button" className="btn btn-primary btn-sm" onClick={save}>
                    {t('Save to profile')}
                  </button>
                )}
              </div>
            </div>
          </div>
        </div>
      )}

      <ConfirmModal
        isOpen={isDeleteModalOpen}
        title={t('Remove attributes')}
        message={`${t('Remove')} ${selected.length} ${t('selected attribute(s)?')}`}
        confirmText={t('Delete')}
        cancelText={t('Cancel')}
        confirmVariant="danger"
        onConfirm={handleRemoveConfirm}
        onCancel={() => setIsDeleteModalOpen(false)}
      />
    </div>
  )
}
