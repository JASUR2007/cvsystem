import { useEffect, useState } from 'react'
import Markdown from 'react-markdown'
import { api, dateText, type PositionDetail } from '../shared/api'
import { PageTitle, Status } from '../shared/ui'
import { useApi } from '../shared/useApi'
import type { CurrentUser } from '../auth/api'
import { t } from '../shared/i18n'

type Post = { id: string; authorId: string; author: string; content: string; createdAt: string; updatedAt: string | null }

export default function PositionDetailPage({ id, user }: { id: string; user: CurrentUser | null }) {
  const result = useApi<PositionDetail>(`/positions/${id}`)
  const [tab, setTab] = useState('Overview')
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
    if (!user) { window.location.assign('/login'); return }
    try {
      const result = await api<{ id: string }>(`/positions/${id}/cvs`, { method: 'POST' })
      window.location.assign(`/cvs/${result.id}`)
    } catch (cause) { setMessage(cause instanceof Error ? cause.message : 'Could not create CV.') }
  }

  async function post() {
    if (!text.trim()) return
    try {
      await api(`/positions/${id}/discussion`, { method: 'POST', headers: { 'Content-Type': 'application/json' }, body: JSON.stringify({ content: text }) })
      setText('')
      discussion.reload()
    } catch (cause) { setMessage(cause instanceof Error ? cause.message : 'Could not add post.') }
  }

  if (result.loading || result.error || !result.data) return <Status loading={result.loading} error={result.error} />
  const position = result.data
  return <section className="surface page-surface"><a className="back-link" href="/positions">← {t('Back to positions')}</a><PageTitle title={position.title} action={<div className="action-group">{canCreate && <button className="btn btn-primary" onClick={createCv}>{t('Create CV')}</button>}{canManage && <a href={`/positions/${id}/edit`} className="btn btn-outline-primary">{t('Edit position')}</a>}</div>} /><p className="muted">{position.company || t('Company not specified')} · {position.level || t('Any level')} · {t('Updated')} {dateText(position.updatedAt)}</p>{message && <div className="alert alert-danger">{message}</div>}<div className="step-tabs">{['Overview', 'Discussion', ...(canManage ? ['CVs'] : [])].map(item => <button key={item} className={tab === item ? 'active' : ''} onClick={() => setTab(item)}>{t(item)}</button>)}</div>{tab === 'Overview' && <div className="detail-grid"><div><h2>{t('Description')}</h2><Markdown>{position.shortDescription}</Markdown><h2>{t('CV attributes')}</h2><div className="tag-list">{position.attributes.map(attribute => <span className="tag" key={attribute.attributeId}>{attribute.name}{attribute.isRequired ? ' *' : ''}</span>)}{position.attributes.length === 0 && <span className="muted">{t('No extra attributes')}</span>}</div></div><aside><h2>{t('Project requirements')}</h2><p>{t('Maximum projects')}: {position.maxProjects}</p><div className="tag-list">{position.projectTags.map(tag => <span className="tag" key={tag}>{tag}</span>)}</div><h2>{t('Access')}</h2><p>{t(position.isPublic ? 'Public to authenticated candidates' : 'Restricted')}</p>{position.accessRules.map(rule => <p key={rule.id} className="rule-summary">{rule.attributeName} {rule.operator} {rule.comparisonValue}</p>)}</aside></div>}{tab === 'Discussion' && <div className="discussion"><Status loading={discussion.loading} error={discussion.error} empty={discussion.data?.length === 0} />{discussion.data?.map(item => <article key={item.id} className="post"><div><strong>{item.author}</strong><time>{dateText(item.createdAt)}</time></div><Markdown>{item.content}</Markdown></article>)}{user ? <div className="post-form"><textarea className="form-control" value={text} onChange={event => setText(event.target.value)} rows={3} placeholder={t('Write a message in Markdown')} /><button className="btn btn-primary" onClick={post}>{t('Post message')}</button></div> : <p><a href="/login">{t('Sign in')}</a> {t('to join the discussion.')}</p>}</div>}{tab === 'CVs' && <div><p>{t('Compare published CVs for this position.')}</p><a className="btn btn-primary" href={`/positions/${id}/cvs`}>{t('Open CV table')}</a></div>}</section>
}
