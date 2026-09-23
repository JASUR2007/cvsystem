import { useState, type FormEvent } from 'react'
import { dateText, type PositionListItem } from '../shared/api'
import { Status } from '../shared/ui'
import { useApi } from '../shared/useApi'
import { t } from '../shared/i18n'
import heroPerson from '../assets/hero-person.jpg'
import '../home.css'

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
      {/* Hero Section (Screen 1 Layout) */}
      <section className="home-hero">
        <div className="hero-copy">
          <span className="eyebrow" style={{ color: 'var(--color-primary)', fontWeight: 600, fontSize: '0.875rem' }}>
            TalentHub Recruitment Platform
          </span>
          <h1>
            {t('Find the right talent.')} <br />
            <span className="hero-gradient-text">{t('Build the right future.')}</span>
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

        {/* Right-side Hero Visual Presentation with Recruiter Image & Floating Badges */}
        <div className="hero-preview-wrapper" aria-hidden="true">
          <div className="hero-backdrop-glow" />

          <div className="hero-image-frame">
            {/* Floating Pills */}
            <div className="floating-hero-pill pill-candidates">
              <span>🎓</span>
              <span>{t('Candidates')}</span>
            </div>

            <div className="floating-hero-pill pill-recruiters">
              <span>👥</span>
              <span>{t('Recruiters')}</span>
            </div>

            <div className="floating-hero-pill pill-opportunities">
              <span>💼</span>
              <span>Opportunities</span>
            </div>

            <img
              src={heroPerson}
              alt="TalentHub Platform Recruiter"
              className="hero-person-img"
            />
          </div>

          {/* Floating mini candidate match preview */}
          <div className="hero-mini-card">
            <div className="mini-card-row">
              <div className="mini-card-user">
                <div className="mini-card-avatar">JK</div>
                <div>
                  <strong style={{ fontSize: '0.875rem', display: 'block' }}>Jasur Karimov</strong>
                  <small className="text-muted">Backend Developer</small>
                </div>
              </div>
              <span className="badge text-bg-success" style={{ fontSize: '0.7rem' }}>
                ✓ {t('Eligible')}
              </span>
            </div>

            <div className="d-flex justify-content-between align-items-center mb-1" style={{ fontSize: '0.75rem' }}>
              <span className="text-muted">{t('Position match')}</span>
              <strong style={{ color: '#059669' }}>92%</strong>
            </div>
            <div className="match-bar-track">
              <div className="match-bar-fill" />
            </div>
          </div>
        </div>
      </section>

      {/* Latest Positions (Full-width Table) */}
      <section className="home-latest">
        <div className="section-head">
          <h2>{t('Latest positions')}</h2>
          <a href="/positions">{t('View all →')}</a>
        </div>
        <Status loading={latest.loading} error={latest.error} empty={latest.data?.length === 0} />
        {latest.data && latest.data.length > 0 && (
          <div className="table-responsive">
            <table className="table table-hover align-middle mb-0">
              <thead>
                <tr>
                  <th>{t('Title')}</th>
                  <th>{t('Company')}</th>
                  <th>{t('Level')}</th>
                  <th>{t('CVs')}</th>
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
                      <a
                        href={`/positions/${position.id}`}
                        style={{ fontWeight: 600, color: 'var(--color-primary)', textDecoration: 'none' }}
                        onClick={e => e.stopPropagation()}
                      >
                        {position.title}
                      </a>
                    </td>
                    <td>{position.company || '—'}</td>
                    <td>
                      <span className="badge text-bg-light border">{position.level || '—'}</span>
                    </td>
                    <td>
                      <span className="badge bg-primary-subtle text-primary border border-primary-subtle">
                        {position.cvCount}
                      </span>
                    </td>
                    <td>{dateText(position.updatedAt)}</td>
                  </tr>
                ))}
              </tbody>
            </table>
          </div>
        )}
      </section>

      {/* 2-Column: Popular Positions + Platform Statistics */}
      <div className="home-grid">
        {/* Popular Positions */}
        <section className="home-card">
          <div className="section-head">
            <h2>{t('Popular positions')}</h2>
          </div>
          <Status loading={popular.loading} error={popular.error} empty={popular.data?.length === 0} />
          <div className="popular-list">
            {popular.data?.map(item => (
              <a href={`/positions/${item.id}`} key={item.id} className="popular-item">
                <span className="popular-title">{item.title}</span>
                <span className="popular-cv-count">{item.cvCount} CVs</span>
              </a>
            ))}
          </div>
        </section>

        {/* Platform Statistics */}
        <section className="home-card">
          <div className="section-head">
            <h2>{t('Platform statistics')}</h2>
          </div>
          <Status loading={stats.loading} error={stats.error} />
          {stats.data && (
            <div className="stats-grid">
              <div className="stat-item">
                <span className="stat-number">{stats.data.users.toLocaleString()}</span>
                <span className="stat-label">{t('Users')}</span>
              </div>
              <div className="stat-item">
                <span className="stat-number" style={{ color: '#2563EB' }}>
                  {stats.data.candidates.toLocaleString()}
                </span>
                <span className="stat-label">{t('Candidates')}</span>
              </div>
              <div className="stat-item">
                <span className="stat-number" style={{ color: '#059669' }}>
                  {stats.data.recruiters.toLocaleString()}
                </span>
                <span className="stat-label">{t('Recruiters')}</span>
              </div>
              <div className="stat-item">
                <span className="stat-number" style={{ color: '#7C3AED' }}>
                  {stats.data.publishedCvs.toLocaleString()}
                </span>
                <span className="stat-label">{t('Published CVs')}</span>
              </div>
            </div>
          )}
        </section>
      </div>

      {/* Popular Technologies / Tag Cloud */}
      <section className="home-technologies">
        <div className="section-head">
          <h2>{t('Popular technologies')}</h2>
        </div>
        <Status loading={tags.loading} error={tags.error} empty={tags.data?.length === 0} />
        <div className="tag-cloud">
          {tags.data?.map(tag => (
            <a href={`/positions?q=${encodeURIComponent(tag.name)}`} key={tag.name} className="tag-chip">
              <span>{tag.name}</span>
              <span className="tag-count">({tag.count})</span>
            </a>
          ))}
        </div>
      </section>
    </>
  )
}
