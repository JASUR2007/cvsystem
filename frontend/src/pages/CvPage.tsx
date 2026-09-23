import { useState } from 'react'
import Markdown from 'react-markdown'
import { api, dateText, json, type AttributeValue, type CvDetail } from '../shared/api'
import AttributeEditor from '../shared/AttributeEditor'
import { Status } from '../shared/ui'
import { useApi } from '../shared/useApi'
import type { CurrentUser } from '../auth/api'
import { imageUrl } from '../shared/imageUrl'
import { t } from '../shared/i18n'
import '../cv-pages.css'

export default function CvPage({ id, user }: { id: string; user: CurrentUser | null }) {
  const result = useApi<CvDetail>(`/cvs/${id}`)
  const likes = useApi<{ count: number; liked: boolean }>(`/cvs/${id}/likes`)
  const [editing, setEditing] = useState<AttributeValue | null>(null)
  const [message, setMessage] = useState('')
  const [busy, setBusy] = useState(false)

  async function save() {
    if (!editing) return
    setBusy(true)
    try {
      await api(`/cvs/${id}/attributes/${editing.attributeId}`, json('PUT', { version: editing.version, value: editing }))
      setEditing(null)
      result.reload()
      setMessage(t('Value saved.'))
    } catch (cause) {
      setMessage(cause instanceof Error ? cause.message : t('Could not save value.'))
    } finally {
      setBusy(false)
    }
  }

  async function publish() {
    try {
      await api(`/cvs/${id}/publish`, { method: 'POST' })
      result.reload()
      setMessage(t('CV published successfully.'))
    } catch (cause) {
      setMessage(cause instanceof Error ? cause.message : t('Could not publish CV.'))
    }
  }

  async function toggleLike() {
    try {
      await api(`/cvs/${id}/likes`, { method: likes.data?.liked ? 'DELETE' : 'POST' })
      likes.reload()
      result.reload()
    } catch (cause) {
      setMessage(cause instanceof Error ? cause.message : 'Could not update like.')
    }
  }

  if (result.loading || result.error || !result.data) {
    return (
      <div className="cv-page-container">
        <Status loading={result.loading} error={result.error} />
      </div>
    )
  }

  const cv = result.data
  const canLike = user?.roles.some(role => role === 'Recruiter' || role === 'Administrator') && cv.status === 'Published'

  return (
    <div className="cv-page-container">
      {/* Navigation */}
      <a className="back-nav-link" href={`/positions/${cv.positionId}`}>
        <svg width="16" height="16" viewBox="0 0 24 24" fill="none" stroke="currentColor" strokeWidth="2">
          <line x1="19" y1="12" x2="5" y2="12" />
          <polyline points="12 19 5 12 12 5" />
        </svg>
        <span>{t('Back to positions')}</span>
      </a>

      <div className="cv-sheet-card">
        <header className="cv-header-block">
          <div>
            <h1 className="cv-candidate-title">{cv.firstName} {cv.lastName}</h1>
            <p className="cv-subtitle">
              {cv.position} {cv.location && `· ${cv.location}`} · {t('Updated')} {dateText(cv.updatedAt)}
            </p>
          </div>

          <div className="cv-header-actions">
            <span className={`badge ${cv.status === 'Published' ? 'text-bg-success' : 'text-bg-warning'}`}>
              {t(cv.status)}
            </span>

            {cv.canEdit && cv.status === 'Draft' && (
              <button className="btn btn-primary btn-sm" onClick={publish}>
                {t('Publish')}
              </button>
            )}

            {canLike && (
              <button className="btn btn-outline-danger btn-sm" onClick={toggleLike}>
                {likes.data?.liked ? `♥ ${t('Liked')}` : `♡ ${t('Like')}`} ({likes.data?.count ?? cv.likes})
              </button>
            )}
          </div>
        </header>

        {message && (
          <div className="alert alert-info alert-dismissible fade show" role="alert">
            {message}
            <button type="button" className="btn-close" onClick={() => setMessage('')}></button>
          </div>
        )}

        {/* Section 1: Personal Information */}
        <section className="cv-section-block">
          <h2 className="cv-section-title">{t('Personal Information')}</h2>
          <table className="cv-kv-table">
            <tbody>
              <tr>
                <th>{t('First name')}</th>
                <td>{cv.firstName}</td>
              </tr>
              <tr>
                <th>{t('Last name')}</th>
                <td>{cv.lastName}</td>
              </tr>
              <tr>
                <th>{t('Location')}</th>
                <td>{cv.location || t('Location not set')}</td>
              </tr>
            </tbody>
          </table>
        </section>

        {/* Section 2: Professional Information (Attributes) */}
        <section className="cv-section-block">
          <h2 className="cv-section-title">{t('Professional information')}</h2>
          <table className="cv-kv-table">
            <tbody>
              {cv.attributes.map(attribute => {
                const val = attribute.value
                const isFilled = attribute.isFilled
                const displayVal =
                  val.type === 'Image' && imageUrl(val.imageObjectKey) ? (
                    <img className="upload-preview" src={imageUrl(val.imageObjectKey)!} alt={val.name} style={{ maxWidth: 120, maxHeight: 120, borderRadius: '0.5rem' }} />
                  ) : val.type === 'Text' && val.textValue ? (
                    <Markdown>{val.textValue}</Markdown>
                  ) : (
                    val.textValue ??
                    val.numberValue ??
                    val.dateValue ??
                    val.booleanValue?.toString() ??
                    (val.selectedOptionId ? attribute.options.find(o => o.id === val.selectedOptionId)?.value : null) ??
                    val.imageObjectKey ??
                    (val.periodStart ? `${val.periodStart} – ${val.periodEnd ?? ''}` : null)
                  )

                return (
                  <tr key={val.attributeId}>
                    <th>
                      {val.name} {attribute.isRequired && <span className="text-danger">*</span>}
                    </th>
                    <td>
                      <div className="d-flex align-items-center justify-content-between">
                        <div>
                          {isFilled ? (
                            displayVal
                          ) : (
                            <span className="missing-badge">
                              ⚠ {t('Empty')}
                            </span>
                          )}
                        </div>

                        {cv.canEdit && (
                          <button
                            type="button"
                            className="btn btn-outline-secondary btn-sm"
                            onClick={() => setEditing(val)}
                          >
                            {isFilled ? t('Edit') : `+ ${t('Fill')}`}
                          </button>
                        )}
                      </div>
                    </td>
                  </tr>
                )
              })}
            </tbody>
          </table>
        </section>

        {/* Section 3: Relevant Projects */}
        <section className="cv-section-block">
          <h2 className="cv-section-title">{t('Relevant projects')}</h2>
          {cv.projects.length === 0 ? (
            <p className="text-muted">{t('No matching projects.')}</p>
          ) : (
            cv.projects.map(project => (
              <article className="cv-project-entry" key={project.id}>
                <h4>{project.name}</h4>
                <div className="cv-project-meta">
                  {project.startedOn} – {project.endedOn || t('Present')}
                  {project.tags.length > 0 && ` · ${project.tags.join(', ')}`}
                </div>
                <div style={{ fontSize: '0.875rem', lineHeight: 1.6 }}>
                  <Markdown>{project.description}</Markdown>
                </div>
              </article>
            ))
          )}
        </section>

        {/* Modal Dialog for In-Place Attribute Editing */}
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
              role="document"
              onClick={e => e.stopPropagation()}
            >
              <div className="modal-content shadow-lg border-0 bg-card">
                <div className="modal-header border-bottom">
                  <h5 className="modal-title h6 mb-0" style={{ fontWeight: 600 }}>
                    {t('Edit')} {editing.name}
                  </h5>
                  <button
                    type="button"
                    className="btn-close"
                    onClick={() => setEditing(null)}
                    aria-label="Close"
                  ></button>
                </div>

                <div className="modal-body py-3">
                  <AttributeEditor
                    value={editing}
                    options={cv.attributes.find(item => item.value.attributeId === editing.attributeId)?.options}
                    onChange={setEditing}
                    userId={user?.roles.includes('Administrator') ? cv.candidateId : undefined}
                  />
                </div>

                <div className="modal-footer border-top">
                  <button
                    type="button"
                    className="btn btn-outline-secondary btn-sm"
                    onClick={() => setEditing(null)}
                  >
                    {t('Cancel')}
                  </button>
                  <button
                    type="button"
                    className="btn btn-primary btn-sm"
                    disabled={busy}
                    onClick={save}
                  >
                    {busy ? t('Saving...') : t('Save to profile')}
                  </button>
                </div>
              </div>
            </div>
          </div>
        )}
      </div>
    </div>
  )
}
