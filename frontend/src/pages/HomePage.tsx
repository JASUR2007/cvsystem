import { useState, type FormEvent } from 'react'
import { dateText, type PositionListItem } from '../shared/api'
import { Status } from '../shared/ui'
import { useApi } from '../shared/useApi'
import { t } from '../shared/i18n'

type Stats = {
  users: number
  candidates: number
  recruiters: number
  positions: number
  publishedCvs: number
  cvsLast24Hours: number
}

type Popular = {
  id: string
  title: string
  cvCount: number
}

type Tag = {
  name: string
  count: number
}

export default function HomePage() {
  const [query, setQuery] = useState('')
  const latest = useApi<PositionListItem[]>('/home/latest-positions')
  const popular = useApi<Popular[]>('/home/popular-positions')
  const stats = useApi<Stats>('/home/statistics')
  const tags = useApi<Tag[]>('/home/tags')

  function search(event: FormEvent) {
    event.preventDefault()
    if (!query.trim()) return
    window.location.assign(`/search?q=${encodeURIComponent(query.trim())}`)
  }

  return (
    <>
      {/* Hero Section */}
      <section className="home-hero">
        <div className="hero-copy">
          <span className="eyebrow">TalentHub Recruitment Platform</span>
          <h1>
            Find the right talent. <br />
            <span className="hero-gradient-text">Build the right future.</span>
          </h1>
          <p>
            {t('Structured positions and reusable candidate profiles make every CV relevant.')}
          </p>

          <form className="hero-search-box" onSubmit={search}>
            <input
              className="form-control"
              value={query}
              onChange={e => setQuery(e.target.value)}
              placeholder={t('Search positions or candidates')}
              aria-label={t('Search')}
            />
            <button className="btn btn-primary" type="submit">
              {t('Search')}
            </button>
          </form>
        </div>

        {/* Right-side Mockup Preview Card */}
        <div className="hero-preview-wrapper" aria-hidden="true">
          <div className="hero-backdrop-glow" />
          <div className="candidate-preview-card">
            <div className="candidate-card-top">
              <div className="candidate-avatar-info">
                <div className="preview-avatar">JK</div>
                <div className="preview-candidate-meta">
                  <strong>Jasur Karimov</strong>
                  <span>Backend Developer</span>
                </div>
              </div>
              <span className="eligibility-badge">
                <svg width="14" height="14" viewBox="0 0 24 24" fill="none" stroke="currentColor" strokeWidth="3">
                  <polyline points="20 6 9 17 4 12" />
                </svg>
                {t('Eligible')}
              </span>
            </div>

            <div className="match-score-section">
              <div className="match-score-header">
                <span>{t('Position match')}</span>
                <strong>92%</strong>
              </div>
              <div className="match-progress-track">
                <div className="match-progress-fill" />
              </div>
            </div>

            <div className="preview-attributes-grid">
              <div className="preview-attr-box">
                <span>English</span>
                <strong>C1</strong>
              </div>
              <div className="preview-attr-box">
                <span>Docker</span>
                <strong>{t('Yes')}</strong>
              </div>
              <div className="preview-attr-box">
                <span>.NET</span>
                <strong>3 {t('years')}</strong>
              </div>
              <div className="preview-attr-box">
                <span>PostgreSQL</span>
                <strong>{t('Yes')}</strong>
              </div>
            </div>

            <div className="preview-tags-row">
              <span className="preview-tag-pill">CV</span>
              <span className="preview-tag-pill">{t('Attributes')}</span>
              <span className="preview-tag-pill">{t('Positions')}</span>
            </div>
          </div>
        </div>
      </section>

      {/* Latest Positions (Full-width) */}
      <section className="surface home-latest">
        <div className="section-head">
          <h2>{t('Latest positions')}</h2>
          <a href="/positions">{t('View all →')}</a>
        </div>
        <Status loading={latest.loading} error={latest.error} empty={latest.data?.length === 0} />
        {latest.data && latest.data.length > 0 && (
          <div className="table-responsive">
            <table className="table table-hover align-middle">
              <thead>
                <tr>
                  <th>{t('Title')}</th>
                  <th>{t('Company')}</th>
                  <th>{t('Level')}</th>
                  <th>{t('Updated')}</th>
                </tr>
              </thead>
              <tbody>
                {latest.data.map(position => (
                  <tr
                    key={position.id}
                    className="click-row"
                    onClick={() => window.location.assign(`/positions/${position.id}`)}
                  >
                    <td>
                      <a href={`/positions/${position.id}`}>{position.title}</a>
                    </td>
                    <td>{position.company || '—'}</td>
                    <td>
                      <span className="level-pill">{position.level || '—'}</span>
                    </td>
                    <td>{dateText(position.updatedAt)}</td>
                  </tr>
                ))}
              </tbody>
            </table>
          </div>
        )}
      </section>

      {/* Popular Positions + Platform Statistics Grid */}
      <div className="home-grid">
        {/* Popular Positions */}
        <section className="surface">
          <div className="section-head">
            <h2>{t('Popular positions')}</h2>
          </div>
          <Status loading={popular.loading} error={popular.error} empty={popular.data?.length === 0} />
          {popular.data && popular.data.length > 0 && (
            <div className="popular-positions-list">
              {popular.data.map((position, index) => (
                <a className="popular-row" href={`/positions/${position.id}`} key={position.id}>
                  <div className="popular-row-left">
                    <span className="popular-rank">{String(index + 1).padStart(2, '0')}</span>
                    <span className="popular-title">{position.title}</span>
                  </div>
                  <span className="popular-cv-count">{position.cvCount} CVs</span>
                </a>
              ))}
            </div>
          )}
        </section>

        {/* Platform Statistics */}
        <section className="surface">
          <div className="section-head">
            <h2>{t('Platform statistics')}</h2>
          </div>
          <Status loading={stats.loading} error={stats.error} />
          {stats.data && (
            <div className="stats-grid">
              <div className="stat-card-item">
                <strong>{stats.data.users}</strong>
                <span>{t('Users')}</span>
              </div>
              <div className="stat-card-item">
                <strong>{stats.data.candidates}</strong>
                <span>{t('Candidates')}</span>
              </div>
              <div className="stat-card-item">
                <strong>{stats.data.recruiters}</strong>
                <span>{t('Recruiters')}</span>
              </div>
              <div className="stat-card-item">
                <strong>{stats.data.positions}</strong>
                <span>{t('Positions')}</span>
              </div>
              <div className="stat-card-item">
                <strong>{stats.data.publishedCvs}</strong>
                <span>{t('Published CVs')}</span>
              </div>
              <div className="stat-card-item">
                <strong>{stats.data.cvsLast24Hours}</strong>
                <span>{t('New CVs today')}</span>
              </div>
            </div>
          )}
        </section>
      </div>

      {/* Popular Technologies (Full-width) */}
      <section className="surface technologies">
        <div className="section-head">
          <h2>{t('Popular technologies')}</h2>
        </div>
        <Status loading={tags.loading} error={tags.error} empty={tags.data?.length === 0} />
        {tags.data && tags.data.length > 0 && (
          <div className="tag-list">
            {tags.data.map(tag => (
              <a className="tech-tag-chip" href={`/search?q=${encodeURIComponent(tag.name)}`} key={tag.name}>
                <span>{tag.name}</span>
                <span className="tech-tag-count">{tag.count}</span>
              </a>
            ))}
          </div>
        )}
      </section>
    </>
  )
}
