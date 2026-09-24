import { useEffect, useRef, useState } from 'react'
import { api, dateText, json, type CvListItem, type Profile } from '../../shared/api'
import { getCachedUser, clearAuth } from '../../auth/api'
import { Status } from '../../shared/ui'
import { useApi } from '../../shared/useApi'
import InfoTab from './InfoTab'
import ProjectsTab from './ProjectsTab'
import { imageUrl } from '../../shared/imageUrl'
import { t } from '../../shared/i18n'
import ConfirmModal from '../../shared/ConfirmModal'
import '../../profile.css'

const LOCATION_SUGGESTIONS = [
  'Tashkent, Uzbekistan',
  'Ташкент, Узбекистан',
  'Toshkent, O\'zbekiston',
  'Samarkand, Uzbekistan',
  'Самарканд, Узбекистан',
  'Samarqand, O\'zbekiston',
  'Bukhara, Uzbekistan',
  'Бухара, Узбекистан',
  'Buxoro, O\'zbekiston',
  'Andijan, Uzbekistan',
  'Андижан, Узбекистан',
  'Andijon, O\'zbekiston',
  'Namangan, Uzbekistan',
  'Наманган, Узбекистан',
  'Namangan, O\'zbekiston',
  'Fergana, Uzbekistan',
  'Фергана, Узбекистан',
  'Farg\'ona, O\'zbekiston',
  'Nukus, Uzbekistan',
  'Нукус, Узбекистан',
  'Nukus, O\'zbekiston',
  'Almaty, Kazakhstan',
  'Алматы, Казахстан',
  'Olmaota, Qozog\'iston',
  'Astana, Kazakhstan',
  'Астана, Казахстан',
  'Ostona, Qozog\'iston',
  'Bishkek, Kyrgyzstan',
  'Бишкек, Кыргызстан',
  'Bishkek, Qirg\'iziston',
  'Moscow, Russia',
  'Москва, Россия',
  'Moskva, Rossiya',
  'London, United Kingdom',
  'Лондон, Великобритания',
  'London, Buyuk Britaniya',
  'Berlin, Germany',
  'Берлин, Германия',
  'Berlin, Germaniya',
  'New York, USA',
  'Нью-Йорк, США',
  'Nyu-York, AQSH',
  'Dubai, UAE',
  'Дубай, ОАЭ',
  'Dubay, BAA',
  'Remote',
  'Удаленно',
  'Masofaviy'
]

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
  const [uploadingPhoto, setUploadingPhoto] = useState(false)
  const [photoError, setPhotoError] = useState('')
  const [detectingLocation, setDetectingLocation] = useState(false)
  const [requestingRecruiter, setRequestingRecruiter] = useState(false)
  const [recruiterMessage, setRecruiterMessage] = useState('')
  const [selectedCvs, setSelectedCvs] = useState<string[]>([])
  const [isDeleteCvModalOpen, setIsDeleteCvModalOpen] = useState(false)
  const fileInputRef = useRef<HTMLInputElement>(null)
  const revision = useRef(0)

  async function handleDeleteCvsConfirm() {
    try {
      await Promise.all(selectedCvs.map(id => api(`/cvs/${id}`, { method: 'DELETE' })))
      setSelectedCvs([])
      cvs.reload()
      setIsDeleteCvModalOpen(false)
    } catch (cause) {
      alert(cause instanceof Error ? cause.message : 'Could not delete CV.')
    }
  }

  const cachedUser = getCachedUser()
  const userRoles = result.data?.roles || cachedUser?.roles || []
  const isRecruiter = userRoles.includes('Recruiter')
  const isAdministrator = userRoles.includes('Administrator')
  const isCandidateOnly = !userId && !isRecruiter && !isAdministrator
  const recruiterStatus = isRecruiter ? 'Approved' : (result.data?.recruiterRequestStatus || 'None')

  useEffect(() => {
    const errorStatus = (result.error as any)?.status || (cvs.error as any)?.status
    if (errorStatus === 401) {
      clearAuth()
      window.location.assign('/login')
    }
  }, [result.error, cvs.error])

  useEffect(() => {
    if (result.data && !dirty) {
      queueMicrotask(() => setForm(result.data))
      if (!userId && cachedUser && result.data.photoObjectKey !== cachedUser.photoObjectKey) {
        const updated = { ...cachedUser, photoObjectKey: result.data.photoObjectKey }
        localStorage.setItem('talenthub_user', JSON.stringify(updated))
        sessionStorage.setItem('talenthub_user', JSON.stringify(updated))
        window.dispatchEvent(new Event('talenthub_user_updated'))
      }
    }
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
          if (!userId && cachedUser) {
            const updated = { ...cachedUser, photoObjectKey: saved.photoObjectKey, firstName: saved.firstName, lastName: saved.lastName }
            localStorage.setItem('talenthub_user', JSON.stringify(updated))
            sessionStorage.setItem('talenthub_user', JSON.stringify(updated))
            window.dispatchEvent(new Event('talenthub_user_updated'))
          }
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
    if (!userId && cachedUser && (field === 'photoObjectKey' || field === 'firstName' || field === 'lastName')) {
      const updated = { ...cachedUser, [field]: value }
      localStorage.setItem('talenthub_user', JSON.stringify(updated))
      sessionStorage.setItem('talenthub_user', JSON.stringify(updated))
      window.dispatchEvent(new Event('talenthub_user_updated'))
    }
  }

  async function handlePhotoUpload(file: File) {
    setPhotoError('')
    if (!['image/jpeg', 'image/png', 'image/webp'].includes(file.type) || file.size > 5 * 1024 * 1024) {
      setPhotoError(t('Choose a JPEG, PNG or WebP image smaller than 5 MB.'))
      return
    }
    setUploadingPhoto(true)
    try {
      let objectKey = ''
      try {
        // Direct upload to backend API (handles ACDN S3 via server-side AWS SDK and robust fallback)
        const formData = new FormData()
        formData.append('file', file)
        const uploaded = await api<{ objectKey: string; publicUrl: string | null }>(
          `/files/upload${userId ? `?userId=${userId}` : ''}`,
          { method: 'POST', body: formData }
        )
        objectKey = uploaded.publicUrl || uploaded.objectKey
      } catch {
        // Client-side fallback: Data URL
        objectKey = await new Promise<string>((resolve, reject) => {
          const reader = new FileReader()
          reader.onload = () => resolve(reader.result as string)
          reader.onerror = reject
          reader.readAsDataURL(file)
        })
      }
      change('photoObjectKey', objectKey)
    } catch (cause) {
      setPhotoError(cause instanceof Error ? cause.message : 'Image upload failed.')
    } finally {
      setUploadingPhoto(false)
    }
  }

  function fallbackLocationDetection() {
    try {
      const tz = Intl.DateTimeFormat().resolvedOptions().timeZone
      const lang = localStorage.getItem('talenthub_language') || 'ru'
      if (tz.includes('Tashkent') || tz.includes('Samarkand')) {
        change('location', lang === 'uz' ? "Toshkent, O'zbekiston" : lang === 'ru' ? 'Ташкент, Узбекистан' : 'Tashkent, Uzbekistan')
      } else if (tz.includes('Almaty')) {
        change('location', lang === 'uz' ? "Olmaota, Qozog'iston" : lang === 'ru' ? 'Алматы, Казахстан' : 'Almaty, Kazakhstan')
      } else if (tz.includes('Moscow')) {
        change('location', lang === 'uz' ? 'Moskva, Rossiya' : lang === 'ru' ? 'Москва, Россия' : 'Moscow, Russia')
      } else if (tz.includes('London')) {
        change('location', lang === 'uz' ? 'London, Buyuk Britaniya' : lang === 'ru' ? 'Лондон, Великобритания' : 'London, United Kingdom')
      } else if (tz.includes('New_York')) {
        change('location', lang === 'uz' ? 'Nyu-York, AQSH' : lang === 'ru' ? 'Нью-Йорк, США' : 'New York, USA')
      } else if (tz.includes('Berlin')) {
        change('location', lang === 'uz' ? 'Berlin, Germaniya' : lang === 'ru' ? 'Берлин, Германия' : 'Berlin, Germany')
      } else if (tz.includes('Dubai')) {
        change('location', lang === 'uz' ? 'Dubay, BAA' : lang === 'ru' ? 'Дубай, ОАЭ' : 'Dubai, UAE')
      } else {
        change('location', lang === 'uz' ? "Toshkent, O'zbekiston" : lang === 'ru' ? 'Ташкент, Узбекистан' : 'Tashkent, Uzbekistan')
      }
    } finally {
      setDetectingLocation(false)
    }
  }

  async function handleDetectLocation() {
    setDetectingLocation(true)
    const lang = localStorage.getItem('talenthub_language') || 'ru'
    const acceptLang = lang === 'uz' ? 'uz,ru,en' : lang === 'ru' ? 'ru,uz,en' : 'en,ru,uz'
    try {
      if ('geolocation' in navigator) {
        navigator.geolocation.getCurrentPosition(
          async position => {
            const { latitude, longitude } = position.coords
            try {
              const res = await fetch(
                `https://nominatim.openstreetmap.org/reverse?lat=${latitude}&lon=${longitude}&format=json&accept-language=${acceptLang}`
              )
              const data = await res.json()
              const city =
                data.address?.city ||
                data.address?.town ||
                data.address?.village ||
                data.address?.state ||
                ''
              const country = data.address?.country || ''
              const detected = [city, country].filter(Boolean).join(', ')
              if (detected) {
                change('location', detected)
                setDetectingLocation(false)
                return
              }
            } catch {
              // fallback
            }
            fallbackLocationDetection()
          },
          () => {
            fallbackLocationDetection()
          },
          { timeout: 5000 }
        )
      } else {
        fallbackLocationDetection()
      }
    } catch {
      fallbackLocationDetection()
    }
  }

  async function handleRequestRecruiter() {
    setRequestingRecruiter(true)
    setRecruiterMessage('')
    try {
      const res = await api<{ message?: string; recruiterRequestStatus?: string }>('/profile/request-recruiter', { method: 'POST' })
      setRecruiterMessage(res.message || t('Request submitted. Pending administrator review.'))
      result.reload()
    } catch (cause) {
      setRecruiterMessage(cause instanceof Error ? cause.message : 'Could not request recruiter role.')
    } finally {
      setRequestingRecruiter(false)
    }
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

      {tab === 'Me' && form && (
        <div className="profile-me-grid">
          {/* Left Avatar Card */}
          <div className="profile-avatar-card">
            <div className="profile-avatar-wrapper">
              <div className="profile-large-avatar">
                {imageUrl(form.photoObjectKey) ? (
                  <img src={imageUrl(form.photoObjectKey)!} alt="Profile photo" />
                ) : (
                  <span>{(form.firstName[0] || 'U').toUpperCase()}{(form.lastName[0] || '').toUpperCase()}</span>
                )}
              </div>
              {form.photoObjectKey && (
                <button
                  type="button"
                  className="profile-avatar-remove-badge"
                  title={t('Remove photo')}
                  aria-label={t('Remove photo')}
                  onClick={() => change('photoObjectKey', null)}
                >
                  <svg width="15" height="15" viewBox="0 0 24 24" fill="none" stroke="currentColor" strokeWidth="2.5" strokeLinecap="round" strokeLinejoin="round">
                    <line x1="18" y1="6" x2="6" y2="18" />
                    <line x1="6" y1="6" x2="18" y2="18" />
                  </svg>
                </button>
              )}
              <button
                type="button"
                className="profile-avatar-badge"
                title={t('Change photo')}
                onClick={() => fileInputRef.current?.click()}
                disabled={uploadingPhoto}
              >
                {uploadingPhoto ? (
                  <span className="spinner-border spinner-border-sm" role="status" aria-hidden="true" style={{ width: 14, height: 14 }} />
                ) : (
                  <svg width="18" height="18" viewBox="0 0 24 24" fill="none" stroke="currentColor" strokeWidth="2" strokeLinecap="round" strokeLinejoin="round">
                    <path d="M23 19a2 2 0 0 1-2 2H3a2 2 0 0 1-2-2V8a2 2 0 0 1 2-2h4l2-3h6l2 3h4a2 2 0 0 1 2 2z" />
                    <circle cx="12" cy="13" r="4" />
                  </svg>
                )}
              </button>
              <input
                ref={fileInputRef}
                type="file"
                accept="image/jpeg,image/png,image/webp"
                style={{ display: 'none' }}
                onChange={e => {
                  const file = e.target.files?.[0]
                  if (file) void handlePhotoUpload(file)
                  e.target.value = ''
                }}
              />
            </div>

            {photoError && <small className="text-danger mb-2" role="alert">{photoError}</small>}

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

            <div className="d-flex flex-wrap gap-1 justify-content-center mt-2">
              {userRoles.map(r => (
                <span className="badge text-bg-light border" key={r}>
                  {t(r)}
                </span>
              ))}
            </div>

            {isRecruiter && (
              <div className="mt-3 p-2 rounded border border-success-subtle bg-success-subtle text-start" style={{ fontSize: '0.8125rem' }}>
                <div className="d-flex align-items-center gap-1 fw-semibold text-success">
                  <svg width="14" height="14" viewBox="0 0 24 24" fill="none" stroke="currentColor" strokeWidth="2.5" strokeLinecap="round" strokeLinejoin="round" aria-hidden="true">
                    <polyline points="20 6 9 17 4 12" />
                  </svg>
                  <span>{t('Recruiter')} ({t('Approved')})</span>
                </div>
              </div>
            )}

            {isCandidateOnly && (
              <>
                {recruiterStatus === 'Pending' ? (
                  <div className="mt-3 p-2 rounded border border-warning-subtle bg-warning-subtle text-start" style={{ fontSize: '0.8125rem' }}>
                    <div className="d-flex align-items-center justify-content-between mb-1">
                      <span className="fw-semibold text-warning-emphasis">{t('Recruiter role')}</span>
                      <span className="badge bg-warning text-dark d-inline-flex align-items-center gap-1">
                        <svg width="12" height="12" viewBox="0 0 24 24" fill="none" stroke="currentColor" strokeWidth="2.5" strokeLinecap="round" strokeLinejoin="round" aria-hidden="true">
                          <circle cx="12" cy="12" r="10" />
                          <polyline points="12 6 12 12 16 14" />
                        </svg>
                        {t('Pending')}
                      </span>
                    </div>
                    <p className="text-muted small mb-0">{t('Your request for the recruiter role is currently pending administrator review.')}</p>
                    {recruiterMessage && <div className="text-success small mt-1">{recruiterMessage}</div>}
                  </div>
                ) : recruiterStatus === 'Rejected' ? (
                  <div className="mt-3 p-2 rounded border border-danger-subtle bg-danger-subtle text-start" style={{ fontSize: '0.8125rem' }}>
                    <div className="d-flex align-items-center justify-content-between mb-1">
                      <span className="fw-semibold text-danger-emphasis">{t('Recruiter role')}</span>
                      <span className="badge bg-danger d-inline-flex align-items-center gap-1">
                        <svg width="12" height="12" viewBox="0 0 24 24" fill="none" stroke="currentColor" strokeWidth="2.5" strokeLinecap="round" strokeLinejoin="round" aria-hidden="true">
                          <circle cx="12" cy="12" r="10" />
                          <line x1="15" y1="9" x2="9" y2="15" />
                          <line x1="9" y1="9" x2="15" y2="15" />
                        </svg>
                        {t('Rejected')}
                      </span>
                    </div>
                    <p className="text-muted small mb-2">{t('Your previous request was rejected by an administrator.')}</p>
                    <button
                      type="button"
                      className="btn btn-outline-danger btn-sm w-100"
                      onClick={handleRequestRecruiter}
                      disabled={requestingRecruiter}
                    >
                      {requestingRecruiter ? t('Requesting...') : t('Request again')}
                    </button>
                    {recruiterMessage && <div className="text-secondary small mt-1">{recruiterMessage}</div>}
                  </div>
                ) : (
                  <div className="mt-3 p-2 rounded border bg-light text-start" style={{ fontSize: '0.8125rem' }}>
                    <div className="fw-semibold text-primary mb-1">{t('Looking to hire?')}</div>
                    <p className="text-muted small mb-2">{t('Get recruiter access to publish positions and search candidates.')}</p>
                    <button
                      type="button"
                      className="btn btn-outline-primary btn-sm w-100"
                      onClick={handleRequestRecruiter}
                      disabled={requestingRecruiter}
                    >
                      {requestingRecruiter ? t('Requesting...') : t('Request Recruiter role')}
                    </button>
                    {recruiterMessage && <div className="text-success small mt-1">{recruiterMessage}</div>}
                  </div>
                )}
              </>
            )}
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
                <div className="input-group">
                  <input
                    className="form-control"
                    list="location-suggestions"
                    value={form.location ?? ''}
                    onChange={e => change('location', e.target.value)}
                    placeholder={t('e.g. Tashkent, Uzbekistan')}
                    maxLength={200}
                  />
                  <button
                    type="button"
                    className="btn btn-outline-secondary d-inline-flex align-items-center gap-1"
                    onClick={handleDetectLocation}
                    disabled={detectingLocation}
                    title={t('Detect current location')}
                  >
                    {detectingLocation ? (
                      <span className="spinner-border spinner-border-sm" role="status" aria-hidden="true" />
                    ) : (
                      <svg width="15" height="15" viewBox="0 0 24 24" fill="none" stroke="currentColor" strokeWidth="2">
                        <circle cx="12" cy="12" r="10" />
                        <line x1="12" y1="2" x2="12" y2="6" />
                        <line x1="12" y1="18" x2="12" y2="22" />
                        <line x1="2" y1="12" x2="6" y2="12" />
                        <line x1="18" y1="12" x2="22" y2="12" />
                        <circle cx="12" cy="12" r="3" />
                      </svg>
                    )}
                    <span>{t('Detect')}</span>
                  </button>
                </div>
                <datalist id="location-suggestions">
                  {LOCATION_SUGGESTIONS.map(loc => (
                    <option key={loc} value={loc} />
                  ))}
                </datalist>
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

      {tab === 'Info' && <InfoTab userId={userId} />}

      {tab === 'Projects' && <ProjectsTab userId={userId} />}

      {tab === 'CVs' && (
        <div className="profile-settings-card">
          <div className="profile-card-header">
            <div>
              <h2 className="profile-card-title">{t('CVs')}</h2>
              <p className="text-muted mb-0" style={{ fontSize: '0.875rem' }}>
                {t('Manage tailored CVs generated for accessible positions.')}
              </p>
            </div>
            <div className="d-flex gap-2">
              {selectedCvs.length === 1 && (
                <a
                  href={`/cvs/${selectedCvs[0]}`}
                  className="btn btn-outline-primary btn-sm d-inline-flex align-items-center gap-1"
                  title={t('Edit')}
                >
                  <svg width="14" height="14" viewBox="0 0 24 24" fill="none" stroke="currentColor" strokeWidth="2">
                    <path d="M11 4H4a2 2 0 0 0-2 2v14a2 2 0 0 0 2 2h14a2 2 0 0 0 2-2v-7" />
                    <path d="M18.5 2.5a2.121 2.121 0 0 1 3 3L12 15l-4 1 1-4 9.5-9.5z" />
                  </svg>
                  <span className="btn-responsive-text">{t('Edit')}</span>
                </a>
              )}
              {selectedCvs.length > 0 && (
                <button
                  type="button"
                  className="btn btn-outline-danger btn-sm d-inline-flex align-items-center gap-1"
                  title={t('Delete')}
                  onClick={() => setIsDeleteCvModalOpen(true)}
                >
                  <svg width="14" height="14" viewBox="0 0 24 24" fill="none" stroke="currentColor" strokeWidth="2">
                    <polyline points="3 6 5 6 21 6" />
                    <path d="M19 6v14a2 2 0 0 1-2 2H7a2 2 0 0 1-2-2V6m3 0V4a2 2 0 0 1 2-2h4a2 2 0 0 1 2 2v2" />
                  </svg>
                  <span className="btn-responsive-text">{t('Delete')} ({selectedCvs.length})</span>
                </button>
              )}
              <a href="/positions" className="btn btn-primary btn-sm d-inline-flex align-items-center gap-1" title={t('Create CV')}>
                <svg width="14" height="14" viewBox="0 0 24 24" fill="none" stroke="currentColor" strokeWidth="2">
                  <line x1="12" y1="5" x2="12" y2="19" />
                  <line x1="5" y1="12" x2="19" y2="12" />
                </svg>
                <span className="btn-responsive-text">{t('Create CV')}</span>
              </a>
            </div>
          </div>

          <Status loading={cvs.loading} error={cvs.error} empty={cvs.data?.length === 0} />

          {cvs.data && cvs.data.length > 0 && (
            <div className="table-responsive">
              <table className="table table-hover align-middle mb-0">
                <thead>
                  <tr>
                    <th style={{ width: 44 }}>
                      <input
                        type="checkbox"
                        className="form-check-input"
                        aria-label="Select all"
                        checked={selectedCvs.length > 0 && selectedCvs.length === cvs.data.length}
                        onChange={e =>
                          setSelectedCvs(e.target.checked ? cvs.data!.map(c => c.id) : [])
                        }
                      />
                    </th>
                    <th>{t('Position')}</th>
                    <th>{t('Status')}</th>
                    <th>{t('Likes')}</th>
                    <th>{t('Updated')}</th>
                  </tr>
                </thead>
                <tbody>
                  {cvs.data.map(cv => (
                    <tr
                      key={cv.id}
                      className="click-row"
                      onClick={() => window.location.assign(`/cvs/${cv.id}`)}
                    >
                      <td onClick={e => e.stopPropagation()}>
                        <input
                          type="checkbox"
                          className="form-check-input"
                          aria-label={`Select ${cv.position}`}
                          checked={selectedCvs.includes(cv.id)}
                          onChange={e =>
                            setSelectedCvs(
                              e.target.checked
                                ? [...selectedCvs, cv.id]
                                : selectedCvs.filter(id => id !== cv.id)
                            )
                          }
                        />
                      </td>
                      <td>
                        <a
                          href={`/cvs/${cv.id}`}
                          className="fw-bold text-decoration-none"
                          style={{ color: 'var(--text-primary)' }}
                          onClick={e => e.stopPropagation()}
                        >
                          {cv.position}
                        </a>
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
                    </tr>
                  ))}
                </tbody>
              </table>
            </div>
          )}
        </div>
      )}

      <ConfirmModal
        isOpen={isDeleteCvModalOpen}
        title={t('Delete')}
        message={`${t('Are you sure you want to delete')} ${selectedCvs.length} ${t('selected CV(s)?')}`}
        confirmText={t('Delete')}
        cancelText={t('Cancel')}
        confirmVariant="danger"
        onConfirm={handleDeleteCvsConfirm}
        onCancel={() => setIsDeleteCvModalOpen(false)}
      />
    </div>
  )
}
