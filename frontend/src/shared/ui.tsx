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
  if (total <= 1) return null
  return <div className="pager"><button className="btn btn-outline-primary btn-sm" disabled={page <= 1} onClick={() => onPage(page - 1)}>{t('Previous')}</button><span>{page} / {total}</span><button className="btn btn-outline-primary btn-sm" disabled={page >= total} onClick={() => onPage(page + 1)}>{t('Next')}</button></div>
}

export function PageTitle({ title, action }: { title: string; action?: React.ReactNode }) {
  return <div className="page-title"><h1>{t(title)}</h1>{action}</div>
}
