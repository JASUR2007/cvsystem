import { useState, type FormEvent } from 'react'
import Markdown from 'react-markdown'
import { api, json, type Project } from '../../shared/api'
import { Status } from '../../shared/ui'
import { useApi } from '../../shared/useApi'
import { t } from '../../shared/i18n'

type Form = {
  id?: string
  name: string
  startedOn: string
  endedOn: string
  description: string
  tags: string
  version: number
}

const blank: Form = {
  name: '',
  startedOn: '',
  endedOn: '',
  description: '',
  tags: '',
  version: 1,
}

export default function ProjectsTab({ userId }: { userId?: string }) {
  const result = useApi<Project[]>(`/projects${userId ? `?userId=${userId}` : ''}`)
  const [form, setForm] = useState<Form | null>(null)
  const [selected, setSelected] = useState<string[]>([])
  const [message, setMessage] = useState('')
  const prefix = form?.tags.split(',').at(-1)?.trim() ?? ''
  const tags = useApi<string[]>(form ? `/tags?prefix=${encodeURIComponent(prefix)}` : null)

  async function save(event: FormEvent) {
    event.preventDefault()
    if (!form) return
    try {
      const body = {
        name: form.name,
        startedOn: form.startedOn,
        endedOn: form.endedOn || null,
        description: form.description,
        tags: form.tags.split(',').map(tag => tag.trim()).filter(Boolean),
        version: form.version,
      }
      await api(
        form.id ? `/projects/${form.id}` : `/projects${userId ? `?userId=${userId}` : ''}`,
        json(form.id ? 'PUT' : 'POST', body)
      )
      setForm(null)
      result.reload()
      setMessage('Project saved.')
    } catch (cause) {
      setMessage(cause instanceof Error ? cause.message : 'Could not save project.')
    }
  }

  async function remove() {
    if (!window.confirm(`Delete ${selected.length} selected project(s)?`)) return
    try {
      await Promise.all(selected.map(id => api(`/projects/${id}`, { method: 'DELETE' })))
      setSelected([])
      result.reload()
      setMessage('Projects deleted.')
    } catch (cause) {
      setMessage(cause instanceof Error ? cause.message : 'Could not delete projects.')
    }
  }

  return (
    <div className="profile-settings-card">
      <div className="profile-card-header">
        <div>
          <h2 className="profile-card-title">{t('Projects')}</h2>
          <p className="text-muted mb-0" style={{ fontSize: '0.875rem' }}>
            {t('List notable projects to demonstrate practical experience on generated CVs.')}
          </p>
        </div>

        <div className="d-flex gap-2">
          {selected.length === 1 && (
            <button
              type="button"
              className="btn btn-outline-primary btn-sm"
              onClick={() => {
                const project = result.data?.find(p => p.id === selected[0])
                if (project) {
                  setForm({
                    id: project.id,
                    name: project.name,
                    startedOn: project.startedOn,
                    endedOn: project.endedOn ?? '',
                    description: project.description,
                    tags: project.tags.join(', '),
                    version: project.version,
                  })
                }
              }}
            >
              {t('Edit')}
            </button>
          )}
          {selected.length > 0 && (
            <button className="btn btn-outline-danger btn-sm" onClick={remove}>
              {t('Delete')} ({selected.length})
            </button>
          )}
          <button className="btn btn-primary btn-sm" onClick={() => setForm(blank)}>
            + {t('Add project')}
          </button>
        </div>
      </div>

      {message && (
        <div className="alert alert-info alert-dismissible fade show" role="alert">
          {message}
          <button type="button" className="btn-close" onClick={() => setMessage('')}></button>
        </div>
      )}

      <Status loading={result.loading} error={result.error} empty={result.data?.length === 0} />

      {result.data && result.data.length > 0 && (
        <div className="table-responsive">
          <table className="table table-hover align-middle mb-0">
            <thead>
              <tr>
                <th style={{ width: 40 }}>
                  <input
                    type="checkbox"
                    className="form-check-input"
                    aria-label="Select all"
                    checked={selected.length === result.data.length}
                    onChange={e =>
                      setSelected(e.target.checked ? result.data!.map(p => p.id) : [])
                    }
                  />
                </th>
                <th>{t('Name')}</th>
                <th>{t('Period')}</th>
                <th>{t('Tags')}</th>
              </tr>
            </thead>
            <tbody>
              {result.data.map(project => {
                const startYear = project.startedOn ? project.startedOn.slice(0, 4) : ''
                const endYear = project.endedOn ? project.endedOn.slice(0, 4) : t('Present')
                const periodText = startYear ? `${startYear} – ${endYear}` : ''

                return (
                  <tr
                    key={project.id}
                    className="click-row"
                    onClick={() =>
                      setForm({
                        id: project.id,
                        name: project.name,
                        startedOn: project.startedOn,
                        endedOn: project.endedOn ?? '',
                        description: project.description,
                        tags: project.tags.join(', '),
                        version: project.version,
                      })
                    }
                  >
                    <td onClick={e => e.stopPropagation()}>
                      <input
                        type="checkbox"
                        className="form-check-input"
                        aria-label={`Select ${project.name}`}
                        checked={selected.includes(project.id)}
                        onChange={e =>
                          setSelected(
                            e.target.checked
                              ? [...selected, project.id]
                              : selected.filter(id => id !== project.id)
                          )
                        }
                      />
                    </td>
                    <td>
                      <strong>{project.name}</strong>
                    </td>
                    <td>
                      <small className="text-muted">{periodText}</small>
                    </td>
                    <td>
                      <div className="badge-tag-cloud">
                        {project.tags.slice(0, 3).map(tag => (
                          <span className="tech-tag-pill" key={tag}>
                            {tag}
                          </span>
                        ))}
                        {project.tags.length > 3 && (
                          <span className="tech-tag-pill more">
                            +{project.tags.length - 3}
                          </span>
                        )}
                      </div>
                    </td>
                  </tr>
                )
              })}
            </tbody>
          </table>
        </div>
      )}

      {/* Add / Edit Project Modal */}
      {form && (
        <div
          className="modal fade show d-block"
          tabIndex={-1}
          role="dialog"
          aria-modal="true"
          style={{ backgroundColor: 'rgba(0, 0, 0, 0.5)', zIndex: 1050 }}
          onClick={() => setForm(null)}
        >
          <div
            className="modal-dialog modal-dialog-centered modal-lg"
            role="document"
            onClick={e => e.stopPropagation()}
          >
            <form className="modal-content shadow-lg border-0 bg-card" onSubmit={save}>
              <div className="modal-header border-bottom">
                <h5 className="modal-title h5 mb-0" style={{ fontWeight: 600 }}>
                  {form.id ? t('Edit project') : t('Add project')}
                </h5>
                <button
                  type="button"
                  className="btn-close"
                  onClick={() => setForm(null)}
                  aria-label="Close"
                ></button>
              </div>

              <div className="modal-body">
                <div className="row g-3">
                  <div className="col-12">
                    <label className="form-label" style={{ fontWeight: 600 }}>{t('Name')} *</label>
                    <input
                      className="form-control"
                      value={form.name}
                      onChange={e => setForm({ ...form, name: e.target.value })}
                      required
                      maxLength={200}
                      placeholder={t('e.g. ERP System')}
                    />
                  </div>

                  <div className="col-md-6">
                    <label className="form-label" style={{ fontWeight: 600 }}>{t('Start Date')} *</label>
                    <input
                      type="date"
                      className="form-control"
                      value={form.startedOn}
                      onChange={e => setForm({ ...form, startedOn: e.target.value })}
                      required
                    />
                  </div>

                  <div className="col-md-6">
                    <label className="form-label" style={{ fontWeight: 600 }}>{t('End Date (leave empty if ongoing)')}</label>
                    <input
                      type="date"
                      className="form-control"
                      value={form.endedOn}
                      onChange={e => setForm({ ...form, endedOn: e.target.value })}
                    />
                  </div>

                  <div className="col-12">
                    <label className="form-label" style={{ fontWeight: 600 }}>{t('Technology Tags (comma-separated)')}</label>
                    <input
                      className="form-control"
                      list="tag-suggestions"
                      value={form.tags}
                      onChange={e => setForm({ ...form, tags: e.target.value })}
                      placeholder=".NET, React, PostgreSQL"
                    />
                    <datalist id="tag-suggestions">
                      {tags.data?.map(tag => (
                        <option value={tag} key={tag} />
                      ))}
                    </datalist>
                  </div>

                  <div className="col-12">
                    <label className="form-label" style={{ fontWeight: 600 }}>{t('Description (Markdown)')}</label>
                    <textarea
                      className="form-control"
                      rows={4}
                      value={form.description}
                      onChange={e => setForm({ ...form, description: e.target.value })}
                      placeholder={t('Key contributions and achievements...')}
                    />
                  </div>

                  {form.description && (
                    <div className="col-12">
                      <div className="card p-3 bg-body">
                        <small className="text-muted d-block mb-1">{t('Preview')}:</small>
                        <Markdown>{form.description}</Markdown>
                      </div>
                    </div>
                  )}
                </div>
              </div>

              <div className="modal-footer border-top">
                <button
                  type="button"
                  className="btn btn-outline-secondary"
                  onClick={() => setForm(null)}
                >
                  {t('Cancel')}
                </button>
                <button type="submit" className="btn btn-primary">
                  {t('Save')}
                </button>
              </div>
            </form>
          </div>
        </div>
      )}
    </div>
  )
}
