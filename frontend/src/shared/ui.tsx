import { ApiError } from './api'
import { t } from './i18n'

export function Status({ loading, error, empty }: { loading: boolean; error: Error | null; empty?: boolean }) {
  if (loading) return <div className="state" role="status">{t('Loading...')}</div>
  if (error) {
    const status = error instanceof ApiError ? error.status : 0
    return <div className="alert alert-danger" role="alert">{t(status === 403 ? 'You do not have access to this page.' : status === 404 ? 'The requested item was not found.' : status === 409 ? 'This data changed in another session. Reload to continue.' : error.message)}</div>
  }
  if (empty) return <div className="state">{t('No records found.')}</div>
  return null
}

export function Pager({ page, total, onPage }: { page: number; total: number; onPage: (page: number) => void }) {
  const safeTotal = Math.max(1, total)
  return (
    <div className="pager d-flex align-items-center justify-content-between flex-wrap gap-2 mt-4 pt-3 border-top">
      <div className="text-muted small">
        {t('Page')} <strong>{page}</strong> {t('of')} <strong>{safeTotal}</strong>
      </div>
      <div className="d-flex align-items-center gap-2">
        <button
          type="button"
          className="btn btn-outline-secondary btn-sm"
          disabled={page <= 1}
          onClick={() => onPage(page - 1)}
        >
          ← {t('Previous')}
        </button>
        <span className="badge text-bg-light border px-3 py-2 font-monospace" style={{ fontSize: '0.875rem' }}>
          {page} / {safeTotal}
        </span>
        <button
          type="button"
          className="btn btn-outline-secondary btn-sm"
          disabled={page >= safeTotal}
          onClick={() => onPage(page + 1)}
        >
          {t('Next')} →
        </button>
      </div>
    </div>
  )
}

export function PageTitle({ title, action }: { title: string; action?: React.ReactNode }) {
  return <div className="page-title"><h1>{t(title)}</h1>{action}</div>
}
