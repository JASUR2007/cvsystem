import { useState } from 'react'
import type { PositionCvPage } from '../shared/api'
import { Pager, Status } from '../shared/ui'
import { useApi } from '../shared/useApi'
import { t } from '../shared/i18n'
import '../cv-pages.css'

export default function PositionCvsPage({ id }: { id: string }) {
  const [query, setQuery] = useState('')
  const [attributeId, setAttributeId] = useState('')
  const [operation, setOperation] = useState('Equals')
  const [value, setValue] = useState('')
  const [sort, setSort] = useState('newest')
  const [page, setPage] = useState(1)

  const filter = attributeId && value ? `&attributeId=${attributeId}&operation=${operation}&value=${encodeURIComponent(value)}` : ''
  const result = useApi<PositionCvPage>(
    `/positions/${id}/cvs?q=${encodeURIComponent(query)}${filter}&sort=${sort}&page=${page}`
  )

  return (
    <div className="position-cvs-container">
      {/* Navigation */}
      <a className="back-nav-link" href={`/positions/${id}`}>
        <svg width="16" height="16" viewBox="0 0 24 24" fill="none" stroke="currentColor" strokeWidth="2">
          <line x1="19" y1="12" x2="5" y2="12" />
          <polyline points="12 19 5 12 12 5" />
        </svg>
        <span>{t('Back to positions')}</span>
      </a>

      <div className="d-flex flex-wrap align-items-center justify-content-between gap-3 mb-4">
        <div>
          <h1 className="h3 mb-1" style={{ fontWeight: 700, color: 'var(--text-primary)' }}>
            Candidates ({result.data?.totalItems ?? 0})
          </h1>
          <p className="text-muted mb-0" style={{ fontSize: '0.875rem' }}>
            Structured candidate matrix for position requirements comparison.
          </p>
        </div>

        <div className="d-flex align-items-center gap-2">
          <select
            className="form-select form-select-sm"
            style={{ width: 'auto' }}
            value={sort}
            onChange={e => setSort(e.target.value)}
          >
            <option value="newest">Sort: Newest</option>
            <option value="likes">Sort: Most liked</option>
            <option value="candidate">Sort: Candidate name</option>
          </select>
        </div>
      </div>

      {/* Filter bar */}
      <div className="card p-3 mb-4 border bg-card shadow-sm">
        <div className="row g-2 align-items-center">
          <div className="col-md-4">
            <input
              className="form-control form-control-sm"
              placeholder={t('Search candidate')}
              value={query}
              onChange={e => {
                setQuery(e.target.value)
                setPage(1)
              }}
            />
          </div>

          <div className="col-md-3">
            <select
              className="form-select form-select-sm"
              value={attributeId}
              onChange={e => {
                setAttributeId(e.target.value)
                setPage(1)
              }}
            >
              <option value="">{t('All attributes')}</option>
              {result.data?.columns.map(col => (
                <option value={col.attributeId} key={col.attributeId}>
                  {col.name}
                </option>
              ))}
            </select>
          </div>

          {attributeId && (
            <>
              <div className="col-md-2">
                <select
                  className="form-select form-select-sm"
                  value={operation}
                  onChange={e => setOperation(e.target.value)}
                >
                  {['Equals', 'GreaterThan', 'GreaterThanOrEqual', 'LessThan', 'LessThanOrEqual', 'Contains'].map(
                    item => (
                      <option key={item}>{item}</option>
                    )
                  )}
                </select>
              </div>

              <div className="col-md-3">
                <input
                  className="form-control form-control-sm"
                  placeholder={t('Filter value')}
                  value={value}
                  onChange={e => {
                    setValue(e.target.value)
                    setPage(1)
                  }}
                />
              </div>
            </>
          )}
        </div>
      </div>

      <Status loading={result.loading} error={result.error} empty={result.data?.items.length === 0} />

      {/* Matrix Table */}
      {result.data && result.data.items.length > 0 && (
        <div className="matrix-table-card">
          <div className="table-responsive">
            <table className="table table-hover align-middle mb-0">
              <thead>
                <tr>
                  <th>{t('Candidate')}</th>
                  {result.data.columns.map(column => (
                    <th key={column.attributeId}>{column.name}</th>
                  ))}
                  <th style={{ textAlign: 'right' }}>{t('Likes')}</th>
                </tr>
              </thead>
              <tbody>
                {result.data.items.map(cv => (
                  <tr
                    key={cv.id}
                    className="click-row"
                    onClick={() => window.location.assign(`/cvs/${cv.id}`)}
                  >
                    <td>
                      <a
                        href={`/cvs/${cv.id}`}
                        style={{ fontWeight: 600, color: 'var(--color-primary)', textDecoration: 'none' }}
                        onClick={e => e.stopPropagation()}
                      >
                        {cv.candidate}
                      </a>
                    </td>

                    {result.data!.columns.map(col => {
                      const valItem = cv.values.find(v => v.attributeId === col.attributeId)
                      const isMissing = !valItem?.value

                      return (
                        <td key={col.attributeId}>
                          {isMissing ? (
                            <span className="missing-badge">⚠ {t('Empty')}</span>
                          ) : (
                            <span>{valItem.value}</span>
                          )}
                        </td>
                      )
                    })}

                    <td style={{ textAlign: 'right' }}>
                      <span style={{ color: '#E11D48', fontWeight: 600 }}>♥ {cv.likes}</span>
                    </td>
                  </tr>
                ))}
              </tbody>
            </table>
          </div>
        </div>
      )}

      <div className="mt-4">
        <Pager
          page={page}
          total={Math.ceil((result.data?.totalItems ?? 0) / (result.data?.pageSize ?? 20))}
          onPage={setPage}
        />
      </div>
    </div>
  )
}
