import { useEffect, useRef, useState } from 'react'
import { api, dateText, json, type CvListItem, type Profile } from '../../shared/api'
import { Status } from '../../shared/ui'
import { useApi } from '../../shared/useApi'
import InfoTab from './InfoTab'
import ProjectsTab from './ProjectsTab'
import ImageUploader from '../../shared/ImageUploader'
import { imageUrl } from '../../shared/imageUrl'
import { t } from '../../shared/i18n'
import '../../profile.css'

export default function ProfilePage({ userId }: { userId?: string }) {
  const target = userId ? `?userId=${userId}` : ''
  const result = useApi<Profile>(`/profile${target}`)
  const { setData } = result
  const cvs = useApi<CvListItem[]>(`/cvs${userId ? `?candidateId=${userId}` : ''}`)
  const [tab, setTab] = useState<'Me' | 'Info' | 'Projects' | 'CVs'>('Me')
  const [form, setForm] = useState<Profile | null>(null)
  const [dirty, setDirty] = useState(false)
  const [saveState, setSaveState] = useState('')
  const [savingManual, setSavingManual] = useState(false)
  const revision = useRef(0)

  useEffect(() => {
    if (result.data && !dirty) queueMicrotask(() => setForm(result.data))
  }, [result.data, dirty])

  useEffect(() => {
    if (!dirty || !form) return
    const timer = window.setTimeout(async () => {
      const savingRevision = revision.current
      try {
        setSaveState(t('Saving...'))
        const saved = await api<Profile>(`/profile${target}`, json('PUT', form))
        setData(saved)
        if (revision.current === savingRevision) {
          setForm(saved)
          setDirty(false)
          setSaveState(t('Saved'))
        } else {
          setForm(current => (current ? { ...current, version: saved.version } : current))
          setSaveState(t('Unsaved changes'))
        }
      } catch (cause) {
        setSaveState(cause instanceof Error ? cause.message : 'Save failed.')
      }
    }, 7000)
    return () => window.clearTimeout(timer)
  }, [form, dirty, setData, target])

  function change(field: keyof Profile, value: string | null) {
    if (!form) return
    revision.current++
    setForm({ ...form, [field]: value })
    setDirty(true)
    setSaveState(t('Unsaved changes'))
  }

  async function manualSave() {
    if (!form) return
    setSavingManual(true)
    try {
      const saved = await api<Profile>(`/profile${target}`, json('PUT', form))
      setData(saved)
      setForm(saved)
      setDirty(false)
      setSaveState(t('Saved'))
    } catch (cause) {
      setSaveState(cause instanceof Error ? cause.message : 'Save failed.')
    } finally {
      setSavingManual(false)
    }
  }

  return (
    <div className="profile-container">
      {/* Navigation Tabs */}
      <nav className="profile-nav-tabs" aria-label="Profile Sections">
        {(['Me', 'Info', 'Projects', 'CVs'] as const).map(item => (
          <button
            type="button"
            key={item}
            className={`profile-tab-btn ${tab === item ? 'active' : ''}`}
            onClick={() => setTab(item)}
          >
            {t(item)}
          </button>
        ))}
      </nav>

      {/* Status indicator */}
      <Status loading={result.loading} error={result.error} />

      {/* TAB 1: ME (Screen 7 Layout) */}
      {tab === 'Me' && form && (
        <div className="profile-me-grid">
          {/* Left Avatar Card */}
          <div className="profile-avatar-card">
            <div className="profile-large-avatar">
              {imageUrl(form.photoObjectKey) ? (
                <img src={imageUrl(form.photoObjectKey)!} alt="Profile photo" />
              ) : (
                <span>{(form.firstName[0] || 'U').toUpperCase()}{(form.lastName[0] || '').toUpperCase()}</span>
              )}
            </div>

            <h2 className="profile-display-name">
              {form.firstName} {form.lastName}
            </h2>

            <p className="profile-display-location">
              <svg width="14" height="14" viewBox="0 0 24 24" fill="none" stroke="currentColor" strokeWidth="2">
                <path d="M21 10c0 7-9 13-9 13s-9-6-9-13a9 9 0 0 1 18 0z" />
                <circle cx="12" cy="10" r="3" />
              </svg>
              <span>{form.location || t('Location not set')}</span>
            </p>

            <div style={{ width: '100%', marginTop: '0.5rem' }}>
              <label className="form-label text-muted" style={{ fontSize: '0.75rem', fontWeight: 600 }}>
                {t('Photo')}
              </label>
              <ImageUploader
                value={form.photoObjectKey}
                onChange={key => change('photoObjectKey', key)}
                userId={userId}
              />
            </div>
          </div>

          {/* Right Settings Card */}
          <div className="profile-settings-card">
            <div className="profile-card-header">
              <h2 className="profile-card-title">{t('Professional information')}</h2>
              <span className={`badge ${saveState.includes('failed') ? 'text-bg-danger' : saveState === t('Saved') ? 'text-bg-success' : 'text-bg-secondary'}`}>
                {saveState || t('Changes save automatically after 7 seconds.')}
              </span>
            </div>

            <div className="row g-3 mb-4">
              <div className="col-md-6">
                <label className="form-label" style={{ fontWeight: 600 }}>{t('First name')} *</label>
                <input
                  className="form-control"
                  value={form.firstName}
                  onChange={e => change('firstName', e.target.value)}
                  maxLength={100}
                  required
                />
              </div>

              <div className="col-md-6">
                <label className="form-label" style={{ fontWeight: 600 }}>{t('Last name')} *</label>
                <input
                  className="form-control"
                  value={form.lastName}
                  onChange={e => change('lastName', e.target.value)}
                  maxLength={100}
                  required
                />
              </div>

              <div className="col-12">
                <label className="form-label" style={{ fontWeight: 600 }}>{t('Location')}</label>
                <input
                  className="form-control"
                  value={form.location ?? ''}
                  onChange={e => change('location', e.target.value)}
                  placeholder="e.g. Tashkent, Uzbekistan"
                  maxLength={200}
                />
              </div>

              <div className="col-12">
                <label className="form-label" style={{ fontWeight: 600 }}>{t('Email')}</label>
                <input className="form-control" value={form.email} disabled readOnly />
              </div>
            </div>

            <div className="d-flex align-items-center justify-content-between pt-3 border-top">
              {saveState.includes('another session') ? (
                <button type="button" className="btn btn-warning" onClick={() => window.location.reload()}>
                  {t('Reload profile')}
                </button>
              ) : (
                <button
                  type="button"
                  className="btn btn-primary"
                  onClick={manualSave}
                  disabled={savingManual || !dirty}
                >
                  {savingManual ? t('Saving...') : t('Save')}
                </button>
              )}
              <small className="text-muted">
                {t('Changes save automatically after 7 seconds.')}
              </small>
            </div>
          </div>
        </div>
      )}

      {/* TAB 2: INFO (Screen 8) */}
      {tab === 'Info' && <InfoTab userId={userId} />}

      {/* TAB 3: PROJECTS (Screen 9) */}
      {tab === 'Projects' && <ProjectsTab userId={userId} />}

      {/* TAB 4: CVS (Screen 10 Layout) */}
      {tab === 'CVs' && (
        <div className="profile-settings-card">
          <div className="profile-card-header">
            <h2 className="profile-card-title">{t('CVs')}</h2>
            <a href="/positions" className="btn btn-outline-primary btn-sm">
              + {t('Create CV')}
            </a>
          </div>

          <Status loading={cvs.loading} error={cvs.error} empty={cvs.data?.length === 0} />

          {cvs.data && cvs.data.length > 0 && (
            <div className="table-responsive">
              <table className="table table-hover align-middle mb-0">
                <thead>
                  <tr>
                    <th>{t('Position')}</th>
                    <th>{t('Status')}</th>
                    <th>{t('Likes')}</th>
                    <th>{t('Updated')}</th>
                    <th style={{ textAlign: 'right' }}>Actions</th>
                  </tr>
                </thead>
                <tbody>
                  {cvs.data.map(cv => (
                    <tr
                      key={cv.id}
                      className="click-row"
                      onClick={() => window.location.assign(`/cvs/${cv.id}`)}
                    >
                      <td>
                        <strong style={{ color: 'var(--text-primary)' }}>{cv.position}</strong>
                      </td>
                      <td>
                        <span
                          className={`badge ${cv.status === 'Published' ? 'text-bg-success' : 'text-bg-warning'}`}
                        >
                          {t(cv.status)}
                        </span>
                      </td>
                      <td>
                        <span style={{ color: '#E11D48', fontWeight: 600 }}>♥ {cv.likes}</span>
                      </td>
                      <td>
                        <small className="text-muted">{dateText(cv.updatedAt)}</small>
                      </td>
                      <td style={{ textAlign: 'right' }}>
                        <a
                          href={`/cvs/${cv.id}`}
                          className="btn btn-outline-primary btn-sm"
                          onClick={e => e.stopPropagation()}
                        >
                          View
                        </a>
                      </td>
                    </tr>
                  ))}
                </tbody>
              </table>
            </div>
          )}
        </div>
      )}
    </div>
  )
}
