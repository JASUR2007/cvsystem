import { useState, type FormEvent } from 'react'
import { dateText, type PositionListItem } from '../shared/api'
import { Status } from '../shared/ui'
import { useApi } from '../shared/useApi'
import { t } from '../shared/i18n'

type Stats = { users: number; candidates: number; recruiters: number; positions: number; publishedCvs: number; newCvs: number }
type Popular = { id: string; title: string; cvCount: number }
type Tag = { name: string; count: number }

export default function HomePage() {
  const [query, setQuery] = useState('')
  const latest = useApi<PositionListItem[]>('/home/latest-positions')
  const popular = useApi<Popular[]>('/home/popular-positions')
  const stats = useApi<Stats>('/home/statistics')
  const tags = useApi<Tag[]>('/home/tags')

  function search(event: FormEvent) {
    event.preventDefault()
    window.location.assign(`/search?q=${encodeURIComponent(query)}`)
  }

  return <>
    <section className="home-hero"><div><span className="eyebrow">TALENTHUB</span><h1>{t('Find the right talent.')}<br />{t('Build the right future.')}</h1><p>{t('Structured positions and reusable candidate profiles make every CV relevant.')}</p><form className="hero-form" onSubmit={search}><input className="form-control" value={query} onChange={event => setQuery(event.target.value)} placeholder={t('Search positions or candidates')} aria-label={t('Search')} /><button className="btn btn-primary">{t('Search')}</button></form></div><div className="hero-shape" aria-hidden="true"><span>CV</span><span>ATTRIBUTES</span><span>POSITIONS</span></div></section>
    <div className="home-grid"><section className="surface"><div className="section-head"><h2>{t('Latest positions')}</h2><a href="/positions">{t('View all →')}</a></div><Status loading={latest.loading} error={latest.error} empty={latest.data?.length === 0} />{latest.data && latest.data.length > 0 && <div className="table-responsive"><table className="table table-hover align-middle"><thead><tr><th>{t('Title')}</th><th>{t('Company')}</th><th>{t('Level')}</th><th>{t('Updated')}</th></tr></thead><tbody>{latest.data.map(position => <tr key={position.id} className="click-row" onClick={() => window.location.assign(`/positions/${position.id}`)}><td><a href={`/positions/${position.id}`}>{position.title}</a></td><td>{position.company || '—'}</td><td>{position.level || '—'}</td><td>{dateText(position.updatedAt)}</td></tr>)}</tbody></table></div>}</section><section className="surface"><div className="section-head"><h2>{t('Popular positions')}</h2></div><Status loading={popular.loading} error={popular.error} empty={popular.data?.length === 0} />{popular.data?.map((position, index) => <a className="popular-row" href={`/positions/${position.id}`} key={position.id}><span className="rank">0{index + 1}</span><span>{position.title}</span><strong>{position.cvCount} CVs</strong></a>)}</section></div>
    <div className="home-grid lower"><section className="surface"><div className="section-head"><h2>{t('Popular technologies')}</h2></div><Status loading={tags.loading} error={tags.error} empty={tags.data?.length === 0} /><div className="tag-list">{tags.data?.map(tag => <a className="tag" href={`/search?q=${encodeURIComponent(tag.name)}`} key={tag.name}>{tag.name}</a>)}</div></section><section className="surface"><div className="section-head"><h2>{t('Platform statistics')}</h2></div><Status loading={stats.loading} error={stats.error} /><div className="stats-grid">{stats.data && Object.entries({ Users: stats.data.users, Candidates: stats.data.candidates, Recruiters: stats.data.recruiters, Positions: stats.data.positions, 'Published CVs': stats.data.publishedCvs, 'New CVs today': stats.data.newCvs }).map(([label, value]) => <div key={label}><strong>{value}</strong><span>{t(label)}</span></div>)}</div></section></div>
  </>
}
