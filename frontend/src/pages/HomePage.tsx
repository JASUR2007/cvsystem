import { useState, type FormEvent } from 'react'
import { dateText, type PositionListItem } from '../shared/api'
import { Status } from '../shared/ui'
import { useApi } from '../shared/useApi'
import { t } from '../shared/i18n'

type Stats = { users: number; candidates: number; recruiters: number; positions: number; publishedCvs: number; cvsLast24Hours: number }
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
    <section className="home-hero">
      <div className="hero-copy"><span className="eyebrow">TALENTHUB</span><h1>{t('Find the right talent.')}<br />{t('Build the right future.')}</h1><p>{t('Structured positions and reusable candidate profiles make every CV relevant.')}</p><form className="hero-form" onSubmit={search}><input className="form-control" value={query} onChange={event => setQuery(event.target.value)} placeholder={t('Search positions or candidates')} aria-label={t('Search')} /><button className="btn btn-primary">{t('Search')}</button></form></div>
      <div className="hero-preview" aria-hidden="true">
        <span className="floating-chip chip-attributes">{t('Attributes')}</span><span className="floating-chip chip-position">{t('Positions')}</span><span className="floating-chip chip-cv">CV</span>
        <div className="candidate-card">
          <div className="candidate-heading"><div className="candidate-avatar">JK</div><div><strong>Jasur Karimov</strong><span>Backend Developer</span></div><span className="eligibility">{t('Eligible')} ✓</span></div>
          <div className="candidate-role"><span>{t('Position match')}</span><strong>92%</strong></div>
          <div className="candidate-progress"><span /></div>
          <div className="candidate-attributes"><div><span>English</span><strong>C1</strong></div><div><span>Docker</span><strong>{t('Yes')}</strong></div><div><span>.NET</span><strong>3 {t('years')}</strong></div></div>
          <div className="candidate-tags"><span>.NET</span><span>Docker</span><span>PostgreSQL</span></div>
        </div>
      </div>
    </section>
    <section className="surface home-latest"><div className="section-head"><h2>{t('Latest positions')}</h2><a href="/positions">{t('View all →')}</a></div><Status loading={latest.loading} error={latest.error} empty={latest.data?.length === 0} />{latest.data && latest.data.length > 0 && <div className="table-responsive"><table className="table table-hover align-middle"><thead><tr><th>{t('Title')}</th><th>{t('Company')}</th><th>{t('Level')}</th><th>{t('Updated')}</th></tr></thead><tbody>{latest.data.map(position => <tr key={position.id} className="click-row" onClick={() => window.location.assign(`/positions/${position.id}`)}><td><a href={`/positions/${position.id}`}>{position.title}</a></td><td>{position.company || '—'}</td><td><span className="level-pill">{position.level || '—'}</span></td><td>{dateText(position.updatedAt)}</td></tr>)}</tbody></table></div>}</section>
    <div className="home-grid"><section className="surface"><div className="section-head"><h2>{t('Popular positions')}</h2></div><Status loading={popular.loading} error={popular.error} empty={popular.data?.length === 0} />{popular.data?.map((position, index) => <a className="popular-row" href={`/positions/${position.id}`} key={position.id}><span className="rank">{String(index + 1).padStart(2, '0')}</span><span>{position.title}</span><strong>{position.cvCount} CVs</strong></a>)}</section><section className="surface"><div className="section-head"><h2>{t('Platform statistics')}</h2></div><Status loading={stats.loading} error={stats.error} /><div className="stats-grid">{stats.data && Object.entries({ Users: stats.data.users, Candidates: stats.data.candidates, Recruiters: stats.data.recruiters, Positions: stats.data.positions, 'Published CVs': stats.data.publishedCvs, 'CVs in last 24 hours': stats.data.cvsLast24Hours }).map(([label, value]) => <div key={label}><strong>{value}</strong><span>{t(label)}</span></div>)}</div></section></div>
    <section className="surface technologies"><div className="section-head"><h2>{t('Popular technologies')}</h2></div><Status loading={tags.loading} error={tags.error} empty={tags.data?.length === 0} /><div className="tag-list">{tags.data?.map(tag => <a className="tag" href={`/search?q=${encodeURIComponent(tag.name)}`} key={tag.name}>{tag.name}<span>{tag.count}</span></a>)}</div></section>
  </>
}
