import { useState } from 'react'
import type { PositionCvPage } from '../shared/api'
import { Pager, Status } from '../shared/ui'
import { useApi } from '../shared/useApi'
import { imageUrl } from '../shared/imageUrl'
import ImageViewerModal from '../shared/ImageViewerModal'
import { t } from '../shared/i18n'
import '../cv-pages.css'

export default function PositionCvsPage({ id, embedded = false }: { id: string; embedded?: boolean }) {
  const [query, setQuery] = useState('')
  const [attributeId, setAttributeId] = useState('')
  const [operation, setOperation] = useState('Equals')
  const [value, setValue] = useState('')
  const [sort, setSort] = useState('newest')
  const [page, setPage] = useState(1)
  const [fullscreenImage, setFullscreenImage] = useState<{ url: string; title?: string } | null>(null)

  const filter = attributeId && value ? `&attributeId=${attributeId}&operation=${operation}&value=${encodeURIComponent(value)}` : ''
  const result = useApi<PositionCvPage>(
    `/positions/${id}/cvs?q=${encodeURIComponent(query)}${filter}&sort=${sort}&page=${page}`
  )

  return (
    <div className={`position-cvs-container ${embedded ? 'embedded-cvs-view' : ''}`}>
      {/* Navigation */}
      {!embedded && (
        <a className="back-nav-link" href={`/positions/${id}`}>
          <svg width="16" height="16" viewBox="0 0 24 24" fill="none" stroke="currentColor" strokeWidth="2">
            <line x1="19" y1="12" x2="5" y2="12" />
            <polyline points="12 19 5 12 12 5" />
          </svg>
          <span>{t('Back to positions')}</span>
        </a>
      )}

      <div className="d-flex flex-wrap align-items-center justify-content-between gap-3 mb-4">
        <div>
          <h1 className="h3 mb-1" style={{ fontWeight: 700, color: 'var(--text-primary)' }}>
            {t('Candidates')} ({result.data?.totalItems ?? 0})
          </h1>
          <p className="text-muted mb-0" style={{ fontSize: '0.875rem' }}>
            {t('Structured candidate matrix for position requirements comparison.')}
          </p>
        </div>

        <div className="d-flex align-items-center gap-2">
          <select
            className="form-select form-select-sm"
            style={{ width: 'auto' }}
            value={sort}
            onChange={e => setSort(e.target.value)}
          >
            <option value="newest">{t('Sort: Newest')}</option>
            <option value="likes">{t('Sort: Most liked')}</option>
            <option value="candidate">{t('Sort: Candidate name')}</option>
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
                      const isImageCol = col.type === 'Image' || col.name === 'Personal Photo'

                      return (
                        <td key={col.attributeId}>
                          {isMissing ? (
                            <span className="missing-badge text-muted" style={{ fontSize: '0.85rem' }}>
                              <span style={{ opacity: 0.6, marginRight: '4px' }}>—</span>
                              {t('Empty')}
                            </span>
                          ) : isImageCol && valItem.value ? (
                            <div className="d-flex align-items-center">
                              <img
                                src={imageUrl(valItem.value)!}
                                alt={col.name}
                                style={{
                                  width: '42px',
                                  height: '42px',
                                  objectFit: 'cover',
                                  borderRadius: '7px',
                                  cursor: 'zoom-in',
                                  border: '1px solid var(--bs-border-color)',
                                  backgroundColor: 'var(--bs-tertiary-bg)',
                                  boxShadow: '0 1px 3px rgba(0,0,0,0.1)',
                                  transition: 'transform 0.15s ease',
                                }}
                                title={t('Click to enlarge')}
                                onClick={e => {
                                  e.stopPropagation()
                                  setFullscreenImage({
                                    url: imageUrl(valItem.value)!,
                                    title: `${cv.candidate} — ${col.name}`,
                                  })
                                }}
                              />
                            </div>
                          ) : (
                            <span>
                              {col.type === 'Boolean' || valItem.value === 'true' || valItem.value === 'false'
                                ? valItem.value === 'true'
                                  ? t('Yes')
                                  : t('No')
                                : valItem.value}
                            </span>
                          )}
                        </td>
                      )
                    })}

                    <td style={{ textAlign: 'right' }}>
                      <span className="d-inline-flex align-items-center gap-1" style={{ color: '#E11D48', fontWeight: 600 }}>
                        <svg width="14" height="14" viewBox="0 0 24 24" fill="currentColor">
                          <path d="M12 21.35l-1.45-1.32C5.4 15.36 2 12.28 2 8.5 2 5.42 4.42 3 7.5 3c1.74 0 3.41.81 4.5 2.09C13.09 3.81 14.76 3 16.5 3 19.58 3 22 5.42 22 8.5c0 3.78-3.4 6.86-8.55 11.54L12 21.35z"/>
                        </svg>
                        {cv.likes}
                      </span>
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

      {/* Fullscreen Lightbox Modal */}
      <ImageViewerModal
        url={fullscreenImage?.url ?? null}
        title={fullscreenImage?.title}
        onClose={() => setFullscreenImage(null)}
      />
    </div>
  )
}
