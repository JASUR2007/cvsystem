import { useEffect, useRef, useState } from 'react'
import { api, dateText, json, type CvListItem, type Profile } from '../../shared/api'
import { PageTitle, Status } from '../../shared/ui'
import { useApi } from '../../shared/useApi'
import InfoTab from './InfoTab'
import ProjectsTab from './ProjectsTab'
import ImageUploader from '../../shared/ImageUploader'
import { imageUrl } from '../../shared/imageUrl'

export default function ProfilePage({ userId }: { userId?: string }) {
  const target = userId ? `?userId=${userId}` : ''
  const result = useApi<Profile>(`/profile${target}`)
  const { setData } = result
  const cvs = useApi<CvListItem[]>(`/cvs${userId ? `?candidateId=${userId}` : ''}`)
  const [tab, setTab] = useState('Me')
  const [form, setForm] = useState<Profile | null>(null)
  const [dirty, setDirty] = useState(false)
  const [saveState, setSaveState] = useState('')
  const revision = useRef(0)

  useEffect(() => { if (result.data && !dirty) queueMicrotask(() => setForm(result.data)) }, [result.data, dirty])
  useEffect(() => {
    if (!dirty || !form) return
    const timer = window.setTimeout(async () => {
      const savingRevision = revision.current
      try {
        setSaveState('Saving...')
        const saved = await api<Profile>(`/profile${target}`, json('PUT', form))
        setData(saved)
        if (revision.current === savingRevision) {
          setForm(saved)
          setDirty(false)
          setSaveState('Saved')
        } else {
          setForm(current => current ? { ...current, version: saved.version } : current)
          setSaveState('Unsaved changes')
        }
      } catch (cause) { setSaveState(cause instanceof Error ? cause.message : 'Save failed.') }
    }, 7000)
    return () => window.clearTimeout(timer)
  }, [form, dirty, setData, target])

  function change(field: keyof Profile, value: string | null) {
    if (!form) return
    revision.current++
    setForm({ ...form, [field]: value })
    setDirty(true)
    setSaveState('Unsaved changes')
  }

  return <section className="surface page-surface"><PageTitle title={userId ? 'Candidate profile' : 'My profile'} /><div className="step-tabs">{['Me', 'Info', 'Projects', 'CVs'].map(item => <button key={item} className={tab === item ? 'active' : ''} onClick={() => setTab(item)}>{item}</button>)}</div>{tab === 'Me' && <><Status loading={result.loading} error={result.error} />{form && <div className="profile-grid"><div className="profile-identity"><div className="profile-avatar">{imageUrl(form.photoObjectKey) ? <img src={imageUrl(form.photoObjectKey)!} alt="Profile" /> : <>{form.firstName[0]}{form.lastName[0]}</>}</div><strong>{form.firstName} {form.lastName}</strong><span>{form.email}</span></div><div className="stack-form"><div className="form-grid"><label>First name<input className="form-control" value={form.firstName} onChange={event => change('firstName', event.target.value)} /></label><label>Last name<input className="form-control" value={form.lastName} onChange={event => change('lastName', event.target.value)} /></label><label>Location<input className="form-control" value={form.location ?? ''} onChange={event => change('location', event.target.value)} /></label><label>Photo<ImageUploader value={form.photoObjectKey} onChange={key => change('photoObjectKey', key)} userId={userId} /></label></div><p className={saveState.includes('changed') || saveState.includes('failed') ? 'text-danger' : 'muted'} role="status">{saveState || 'Changes save automatically after 7 seconds.'}</p>{saveState.includes('another session') && <button className="btn btn-outline-primary btn-sm" onClick={() => window.location.reload()}>Reload profile</button>}</div></div>}</>}{tab === 'Info' && <InfoTab userId={userId} />}{tab === 'Projects' && <ProjectsTab userId={userId} />}{tab === 'CVs' && <><Status loading={cvs.loading} error={cvs.error} empty={cvs.data?.length === 0} />{cvs.data && cvs.data.length > 0 && <div className="table-responsive"><table className="table table-hover"><thead><tr><th>Position</th><th>Status</th><th>Likes</th><th>Updated</th></tr></thead><tbody>{cvs.data.map(cv => <tr key={cv.id} className="click-row" onClick={() => window.location.assign(`/cvs/${cv.id}`)}><td><a href={`/cvs/${cv.id}`}>{cv.position}</a></td><td><span className={`badge ${cv.status === 'Published' ? 'text-bg-success' : 'text-bg-warning'}`}>{cv.status}</span></td><td>{cv.likes}</td><td>{dateText(cv.updatedAt)}</td></tr>)}</tbody></table></div>}</>}</section>
}
