import { useEffect, useState } from 'react'
import Markdown from 'react-markdown'
import { api, dateText, type PositionDetail } from '../shared/api'
import { Status } from '../shared/ui'
import { useApi } from '../shared/useApi'
import type { CurrentUser } from '../auth/api'
import { t } from '../shared/i18n'
import '../position-detail.css'

type Post = {
  id: string
  authorId: string
  author: string
  content: string
  createdAt: string
  updatedAt: string | null
}

export default function PositionDetailPage({ id, user }: { id: string; user: CurrentUser | null }) {
  const result = useApi<PositionDetail>(`/positions/${id}`)
  const [tab, setTab] = useState<'Overview' | 'Discussion' | 'CVs'>('Overview')
  const [text, setText] = useState('')
  const [message, setMessage] = useState('')
  const discussion = useApi<Post[]>(tab === 'Discussion' ? `/positions/${id}/discussion` : null)
  const reloadDiscussion = discussion.reload
  const canManage = user?.roles.some(role => role === 'Recruiter' || role === 'Administrator') ?? false
  const canCreate = user?.roles.includes('Candidate') || user?.roles.includes('Administrator')

  useEffect(() => {
    if (tab !== 'Discussion') return
    const timer = window.setInterval(reloadDiscussion, 3000)
    return () => window.clearInterval(timer)
  }, [tab, reloadDiscussion])

  async function createCv() {
    if (!user) {
      window.location.assign('/login')
      return
    }
    try {
      const res = await api<{ id: string }>(`/positions/${id}/cvs`, { method: 'POST' })
      window.location.assign(`/cvs/${res.id}`)
    } catch (cause) {
      setMessage(cause instanceof Error ? cause.message : 'Could not create CV.')
    }
  }

  async function post() {
    if (!text.trim()) return
    try {
      await api(`/positions/${id}/discussion`, {
        method: 'POST',
        headers: { 'Content-Type': 'application/json' },
        body: JSON.stringify({ content: text }),
      })
      setText('')
      discussion.reload()
    } catch (cause) {
      setMessage(cause instanceof Error ? cause.message : 'Could not add post.')
    }
  }

  if (result.loading || result.error || !result.data) {
    return (
      <div className="position-detail-container">
        <Status loading={result.loading} error={result.error} />
      </div>
    )
  }

  const position = result.data

  return (
    <div className="position-detail-container">
      {/* Navigation */}
      <a className="back-nav-link" href="/positions">
        <svg width="16" height="16" viewBox="0 0 24 24" fill="none" stroke="currentColor" strokeWidth="2">
          <line x1="19" y1="12" x2="5" y2="12" />
          <polyline points="12 19 5 12 12 5" />
        </svg>
        <span>{t('Back to positions')}</span>
      </a>

      {/* Header Area */}
      <header className="position-detail-header">
        <div>
          <h1 className="pos-main-title">{position.title}</h1>
          <div className="pos-meta-row">
            {position.company && (
              <span className="pos-meta-item">
                <svg width="15" height="15" viewBox="0 0 24 24" fill="none" stroke="currentColor" strokeWidth="2">
                  <rect x="2" y="7" width="20" height="14" rx="2" ry="2" />
                  <path d="M16 21V5a2 2 0 0 0-2-2h-4a2 2 0 0 0-2 2v16" />
                </svg>
                <strong>{position.company}</strong>
              </span>
            )}
            {position.level && (
              <span className="pos-level-badge">{position.level}</span>
            )}
            <span className="pos-meta-item">
              <svg width="14" height="14" viewBox="0 0 24 24" fill="none" stroke="currentColor" strokeWidth="2">
                <circle cx="12" cy="12" r="10" />
                <polyline points="12 6 12 12 16 14" />
              </svg>
              <span>{t('Updated')} {dateText(position.updatedAt)}</span>
            </span>
            <span className="pos-cv-badge">
              {position.isPublic ? t('Public to authenticated candidates') : t('Restricted')}
            </span>
          </div>
        </div>

        <div className="pos-header-actions">
          {canCreate && (
            <button className="btn btn-primary" onClick={createCv}>
              <svg width="16" height="16" viewBox="0 0 24 24" fill="none" stroke="currentColor" strokeWidth="2">
                <path d="M14 2H6a2 2 0 0 0-2 2v16a2 2 0 0 0 2 2h12a2 2 0 0 0 2-2V8z" />
                <polyline points="14 2 14 8 20 8" />
                <line x1="12" y1="18" x2="12" y2="12" />
                <line x1="9" y1="15" x2="15" y2="15" />
              </svg>
              <span className="btn-responsive-text">{t('Create CV')}</span>
            </button>
          )}
          {canManage && (
            <div className="d-flex gap-2">
              <button
                type="button"
                className="btn btn-outline-secondary d-inline-flex align-items-center gap-1"
                title={t('Duplicate')}
                onClick={async () => {
                  try {
                    const dup = await api<PositionDetail>(`/positions/${id}/duplicate`, { method: 'POST' })
                    window.location.assign(`/positions/${dup.id}`)
                  } catch (cause) {
                    setMessage(cause instanceof Error ? cause.message : 'Could not duplicate position.')
                  }
                }}
              >
                <svg width="16" height="16" viewBox="0 0 24 24" fill="none" stroke="currentColor" strokeWidth="2">
                  <rect x="9" y="9" width="13" height="13" rx="2" ry="2" />
                  <path d="M5 15H4a2 2 0 0 1-2-2V4a2 2 0 0 1 2-2h9a2 2 0 0 1 2 2v1" />
                </svg>
                <span className="btn-responsive-text">{t('Duplicate')}</span>
              </button>
              <a href={`/positions/${id}/edit`} className="btn btn-outline-secondary d-inline-flex align-items-center gap-1" title={t('Edit position')}>
                <svg width="16" height="16" viewBox="0 0 24 24" fill="none" stroke="currentColor" strokeWidth="2">
                  <path d="M11 4H4a2 2 0 0 0-2 2v14a2 2 0 0 0 2 2h14a2 2 0 0 0 2-2v-7" />
                  <path d="M18.5 2.5a2.121 2.121 0 0 1 3 3L12 15l-4 1 1-4 9.5-9.5z" />
                </svg>
                <span className="btn-responsive-text">{t('Edit position')}</span>
              </a>
            </div>
          )}
        </div>
      </header>

      {message && (
        <div className="alert alert-info alert-dismissible fade show" role="alert">
          {message}
          <button type="button" className="btn-close" onClick={() => setMessage('')}></button>
        </div>
      )}

      {/* Tabs */}
      <div className="pos-tabs-bar">
        <button
          type="button"
          className={`pos-tab-btn ${tab === 'Overview' ? 'active' : ''}`}
          onClick={() => setTab('Overview')}
        >
          {t('Overview')}
        </button>
        <button
          type="button"
          className={`pos-tab-btn ${tab === 'Discussion' ? 'active' : ''}`}
          onClick={() => setTab('Discussion')}
        >
          {t('Discussion')}
        </button>
        {canManage && (
          <button
            type="button"
            className={`pos-tab-btn ${tab === 'CVs' ? 'active' : ''}`}
            onClick={() => setTab('CVs')}
          >
            {t('CVs')}
          </button>
        )}
      </div>

      {tab === 'Overview' && (
        <div className="pos-overview-grid">
          {/* Main Left Column */}
          <div className="pos-main-col">
            <div className="pos-card">
              <h2 className="pos-card-title">{t('Description')}</h2>
              <div className="pos-description-body">
                <Markdown>{position.shortDescription || 'No description provided.'}</Markdown>
              </div>
            </div>

            <div className="pos-card">
              <h2 className="pos-card-title">{t('CV attributes')}</h2>
              <div className="pos-attr-chips">
                {position.attributes.map(attr => (
                  <span
                    className={`pos-attr-chip ${attr.isRequired ? 'required' : ''}`}
                    key={attr.attributeId}
                  >
                    <span>{attr.name}</span>
                    {attr.isRequired && <span className="req-star" title="Required">*</span>}
                  </span>
                ))}
                {position.attributes.length === 0 && (
                  <span className="text-muted">{t('No extra attributes')}</span>
                )}
              </div>
            </div>

            <div className="pos-card">
              <h2 className="pos-card-title">{t('Access Requirements')}</h2>
              {position.accessRules.length === 0 ? (
                <p className="text-muted mb-0">{t('Public to authenticated candidates')}</p>
              ) : (
                <table className="pos-rules-table">
                  <tbody>
                    {position.accessRules.map(rule => (
                      <tr key={rule.id}>
                        <td className="pos-rule-name">{rule.attributeName}</td>
                        <td className="pos-rule-operator">{rule.operator}</td>
                        <td className="pos-rule-val">{rule.comparisonValue}</td>
                      </tr>
                    ))}
                  </tbody>
                </table>
              )}
            </div>
          </div>

          {/* Side Right Column */}
          <div className="pos-side-col">
            <div className="pos-card">
              <h2 className="pos-card-title">{t('Project requirements')}</h2>
              <p className="text-muted mb-2">
                <small>{t('Maximum projects')}: <strong>{position.maxProjects}</strong></small>
              </p>
              <div className="pos-attr-chips">
                {position.projectTags.map(tag => (
                  <span className="pos-attr-chip" key={tag}>{tag}</span>
                ))}
                {position.projectTags.length === 0 && (
                  <span className="text-muted"><small>{t('Any projects allowed')}</small></span>
                )}
              </div>
            </div>

            <div className="pos-card">
              <div className="pos-company-header">
                <div className="pos-company-icon">
                  {(position.company?.[0] || 'C').toUpperCase()}
                </div>
                <div>
                  <h3 className="pos-company-title">{position.company || t('Company not specified')}</h3>
                  <p className="pos-company-desc">{t('Verified Hiring Organization')}</p>
                </div>
              </div>
              <p className="text-muted" style={{ fontSize: '0.8125rem' }}>
                {t('Open position on TalentHub platform. Applicants can evaluate compatibility and generate tailored CV.')}
              </p>
            </div>
          </div>
        </div>
      )}

      {/* Discussion Tab Content */}
      {tab === 'Discussion' && (
        <div className="pos-card">
          <h2 className="pos-card-title">{t('Discussion')}</h2>
          <Status loading={discussion.loading} error={discussion.error} empty={discussion.data?.length === 0} />
          
          <div className="pos-discussion-list">
            {discussion.data?.map(item => (
              <article key={item.id} className="pos-post-card">
                <div className="pos-post-header">
                  <a
                    className="pos-post-author"
                    href={canManage ? `/profile/${item.authorId}` : '#'}
                  >
                    <span className="pos-post-avatar">{(item.author[0] || 'U').toUpperCase()}</span>
                    <span>{item.author}</span>
                  </a>
                  <time className="pos-post-time">{dateText(item.createdAt)}</time>
                </div>
                <div className="pos-post-content">
                  <Markdown>{item.content}</Markdown>
                </div>
              </article>
            ))}
          </div>

          {user ? (
            <div className="pos-discussion-form">
              <label className="form-label" style={{ fontWeight: 600 }}>{t('Write a message in Markdown')}</label>
              <textarea
                className="form-control mb-3"
                value={text}
                onChange={event => setText(event.target.value)}
                rows={3}
                placeholder={t('Share a question or note regarding this position...')}
              />
              <button type="button" className="btn btn-primary" onClick={post}>
                {t('Post message')}
              </button>
            </div>
          ) : (
            <p className="text-muted">
              <a href="/login">{t('Sign in')}</a> {t('to join the discussion.')}
            </p>
          )}
        </div>
      )}

      {/* CVs Tab Content (Recruiter/Admin view) */}
      {tab === 'CVs' && (
        <div className="pos-card text-center p-5">
          <h2 className="pos-card-title justify-content-center mb-3">{t('Compare published CVs for this position.')}</h2>
          <p className="text-muted mb-4">
            {t('View structured candidate comparison matrix, filter by skills, and inspect match scores.')}
          </p>
          <a className="btn btn-primary btn-lg" href={`/positions/${id}/cvs`}>
            {t('Open CV table')} →
          </a>
        </div>
      )}
    </div>
  )
}
