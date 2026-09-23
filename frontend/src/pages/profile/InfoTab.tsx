import { useState } from 'react'
import { api, json, type AttributeDetail, type AttributeListItem, type AttributeValue, type Page } from '../../shared/api'
import AttributeEditor from '../../shared/AttributeEditor'
import { Status } from '../../shared/ui'
import { useApi } from '../../shared/useApi'
import { t } from '../../shared/i18n'

export default function InfoTab({ userId }: { userId?: string }) {
  const target = userId ? `?userId=${userId}` : ''
  const result = useApi<AttributeValue[]>(`/profile/attributes${target}`)
  const [lookup, setLookup] = useState('')
  const library = useApi<Page<AttributeListItem>>(`/attributes?prefix=${encodeURIComponent(lookup)}&pageSize=20`)
  const [selected, setSelected] = useState<string[]>([])
  const [editing, setEditing] = useState<AttributeValue | null>(null)
  const definition = useApi<AttributeDetail>(editing ? `/attributes/${editing.attributeId}` : null)
  const [message, setMessage] = useState('')

  async function add(id: string) {
    try {
      await api(`/profile/attributes/${id}${target}`, { method: 'POST' })
      result.reload()
      setMessage('Attribute added.')
    } catch (cause) {
      setMessage(cause instanceof Error ? cause.message : 'Could not add attribute.')
    }
  }

  async function save() {
    if (!editing) return
    try {
      await api(`/profile/attributes/${editing.attributeId}${target}`, json('PUT', { version: editing.version, value: editing }))
      setEditing(null)
      result.reload()
      setMessage('Value saved.')
    } catch (cause) {
      setMessage(cause instanceof Error ? cause.message : 'Could not save value.')
    }
  }

  async function remove() {
    if (!window.confirm(`Remove ${selected.length} selected attribute(s)?`)) return
    try {
      await Promise.all(selected.map(id => api(`/profile/attributes/${id}${target}`, { method: 'DELETE' })))
      setSelected([])
      result.reload()
      setMessage('Attributes removed.')
    } catch (cause) {
      setMessage(cause instanceof Error ? cause.message : 'Could not remove attributes.')
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

        {selected.length > 0 && (
          <button className="btn btn-outline-danger btn-sm" onClick={remove}>
            {t('Delete')} ({selected.length})
          </button>
        )}
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
                <th>Attribute</th>
                <th>Value</th>
                <th style={{ textAlign: 'right' }}>Action</th>
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
                  (value.selectedOptionId ? 'Selected' : value.imageObjectKey ? 'Image' : null)

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
                        <span className="val-empty">(empty)</span>
                      )}
                    </td>
                    <td style={{ textAlign: 'right' }}>
                      <button
                        type="button"
                        className="btn btn-outline-primary btn-sm"
                        onClick={e => {
                          e.stopPropagation()
                          setEditing(value)
                        }}
                      >
                        {valStr ? t('Edit') : '+ Add'}
                      </button>
                    </td>
                  </tr>
                )
              })}
            </tbody>
          </table>
        </div>
      )}

      {/* Inline Edit Panel Modal/Card */}
      {editing && (
        <div className="card mt-4 p-3 bg-secondary-subtle border">
          <div className="d-flex align-items-center justify-content-between mb-3">
            <h3 className="h6 mb-0">
              {t('Edit')} {editing.name}
            </h3>
            <button
              type="button"
              className="btn-close btn-sm"
              onClick={() => setEditing(null)}
              aria-label="Close"
            ></button>
          </div>

          {['First Name', 'Last Name', 'Location', 'Personal Photo'].includes(editing.name) ? (
            <p className="text-muted mb-3">Edit this built-in field on the Me tab.</p>
          ) : (
            <>
              <Status loading={definition.loading} error={definition.error} />
              <AttributeEditor
                value={editing}
                options={definition.data?.options}
                onChange={setEditing}
                userId={userId}
              />
              <div className="d-flex justify-content-end gap-2 mt-3">
                <button
                  type="button"
                  className="btn btn-outline-secondary btn-sm"
                  onClick={() => setEditing(null)}
                >
                  {t('Cancel')}
                </button>
                <button type="button" className="btn btn-primary btn-sm" onClick={save}>
                  {t('Save to profile')}
                </button>
              </div>
            </>
          )}
        </div>
      )}
    </div>
  )
}
