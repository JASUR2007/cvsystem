import { useState } from 'react'
import Markdown from 'react-markdown'
import { api, dateText, json, type AttributeValue, type CvDetail } from '../shared/api'
import AttributeEditor from '../shared/AttributeEditor'
import { PageTitle, Status } from '../shared/ui'
import { useApi } from '../shared/useApi'
import type { CurrentUser } from '../auth/api'
import { imageUrl } from '../shared/imageUrl'
import { t } from '../shared/i18n'

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
      setMessage('Profile value saved.')
    } catch (cause) { setMessage(cause instanceof Error ? cause.message : 'Could not save value.') }
    finally { setBusy(false) }
  }

  async function publish() {
    try { await api(`/cvs/${id}/publish`, { method: 'POST' }); result.reload(); setMessage('CV published.') }
    catch (cause) { setMessage(cause instanceof Error ? cause.message : 'Could not publish CV.') }
  }

  async function toggleLike() {
    try { await api(`/cvs/${id}/likes`, { method: likes.data?.liked ? 'DELETE' : 'POST' }); likes.reload(); result.reload() }
    catch (cause) { setMessage(cause instanceof Error ? cause.message : 'Could not update like.') }
  }

  if (result.loading || result.error || !result.data) return <Status loading={result.loading} error={result.error} />
  const cv = result.data
  const canLike = user?.roles.some(role => role === 'Recruiter' || role === 'Administrator') && cv.status === 'Published'
  return <section className="surface page-surface cv-sheet">
    <a className="back-link" href={`/positions/${cv.positionId}`}>← {t('Position')}</a>
    <PageTitle title={`${cv.firstName} ${cv.lastName}`} action={<div className="action-group"><span className={`badge ${cv.status === 'Published' ? 'text-bg-success' : 'text-bg-warning'}`}>{t(cv.status)}</span>{cv.canEdit && cv.status === 'Draft' && <button className="btn btn-primary" onClick={publish}>{t('Publish')}</button>}{canLike && <button className="btn btn-outline-primary" onClick={toggleLike}>{likes.data?.liked ? `♥ ${t('Liked')}` : `♡ ${t('Like')}`} {likes.data?.count ?? cv.likes}</button>}</div>} />
    <p className="muted">{cv.position} · {cv.location || t('Location not set')} · {t('Updated')} {dateText(cv.updatedAt)}</p>
    {message && <div className="alert alert-info">{message}</div>}
    <div className="cv-section"><h2>{t('Professional information')}</h2>{cv.canEdit && <p className="muted">{t('Select an attribute to edit its value in your profile.')}</p>}<div className="table-responsive"><table className="table"><tbody>{cv.attributes.map(attribute => <tr key={attribute.value.attributeId} className={`${attribute.isRequired && !attribute.isFilled ? 'missing-value' : ''} ${cv.canEdit ? 'click-row' : ''}`} onClick={() => cv.canEdit && setEditing(attribute.value)}><th>{attribute.value.name}</th><td>{attribute.value.type === 'Image' && imageUrl(attribute.value.imageObjectKey) ? <img className="upload-preview" src={imageUrl(attribute.value.imageObjectKey)!} alt={attribute.value.name} /> : attribute.value.type === 'Text' && attribute.value.textValue ? <Markdown>{attribute.value.textValue}</Markdown> : attribute.value.textValue ?? attribute.value.numberValue ?? attribute.value.dateValue ?? attribute.value.booleanValue?.toString() ?? (attribute.value.selectedOptionId ? attribute.options.find(option => option.id === attribute.value.selectedOptionId)?.value : null) ?? attribute.value.imageObjectKey ?? (attribute.value.periodStart ? `${attribute.value.periodStart} – ${attribute.value.periodEnd ?? ''}` : null) ?? `⚠ ${t('Empty')}`}</td></tr>)}</tbody></table></div></div>
    <div className="cv-section"><h2>{t('Relevant projects')}</h2>{cv.projects.length === 0 && <p className="muted">{t('No matching projects.')}</p>}{cv.projects.map(project => <article className="cv-project" key={project.id}><h3>{project.name}</h3><small>{project.startedOn} – {project.endedOn || t('Present')} · {project.tags.join(', ')}</small><Markdown>{project.description}</Markdown></article>)}</div>
    {editing && <div className="edit-panel"><h3>{t('Edit')} {editing.name}</h3><AttributeEditor value={editing} options={cv.attributes.find(item => item.value.attributeId === editing.attributeId)?.options} onChange={setEditing} userId={user?.roles.includes('Administrator') ? cv.candidateId : undefined} /><div className="form-actions"><button className="btn btn-outline-secondary" onClick={() => setEditing(null)}>{t('Cancel')}</button><button className="btn btn-primary" disabled={busy} onClick={save}>{t('Save to profile')}</button></div></div>}
  </section>
}
