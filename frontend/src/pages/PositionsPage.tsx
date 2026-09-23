import { useState } from 'react'
import { api, dateText, type Page, type PositionListItem } from '../shared/api'
import { PageTitle, Pager, Status } from '../shared/ui'
import { useApi } from '../shared/useApi'
import type { CurrentUser } from '../auth/api'
import { t } from '../shared/i18n'

export default function PositionsPage({ user }: { user: CurrentUser | null }) {
  const [query, setQuery] = useState('')
  const [level, setLevel] = useState('')
  const [page, setPage] = useState(1)
  const [selected, setSelected] = useState<string[]>([])
  const [message, setMessage] = useState('')
  const canManage = user?.roles.some(role => role === 'Recruiter' || role === 'Administrator') ?? false
  const result = useApi<Page<PositionListItem>>(`/positions?q=${encodeURIComponent(query)}&level=${level}&page=${page}`)

  async function removeSelected() {
    if (!window.confirm(`Delete ${selected.length} selected position(s)?`)) return
    try {
      await Promise.all(selected.map(id => api(`/positions/${id}`, { method: 'DELETE' })))
      setSelected([])
      setMessage('Selected positions were deleted.')
      result.reload()
    } catch (error) {
      setMessage(error instanceof Error ? error.message : 'Could not delete positions.')
    }
  }

  return <section className="surface page-surface"><PageTitle title="Positions" action={canManage ? <a href="/positions/new" className="btn btn-primary btn-sm">+ {t('Create position')}</a> : undefined} /><div className="toolbar"><input className="form-control" placeholder={t('Search positions')} value={query} onChange={event => { setQuery(event.target.value); setPage(1) }} /><select className="form-select" value={level} onChange={event => { setLevel(event.target.value); setPage(1) }}><option value="">{t('All levels')}</option>{['Junior', 'Middle', 'Senior', 'Lead'].map(item => <option key={item}>{item}</option>)}</select>{canManage && <button className="btn btn-outline-danger" disabled={selected.length === 0} onClick={removeSelected}>{t('Delete selected')}</button>}</div>{message && <div className="alert alert-info">{message}</div>}<Status loading={result.loading} error={result.error} empty={result.data?.items.length === 0} />{result.data && result.data.items.length > 0 && <div className="table-responsive"><table className="table table-hover align-middle"><thead><tr>{canManage && <th><input type="checkbox" aria-label="Select all" checked={selected.length === result.data.items.length} onChange={event => setSelected(event.target.checked ? result.data!.items.map(item => item.id) : [])} /></th>}<th>{t('Title')}</th><th>{t('Company')}</th><th>{t('Level')}</th><th>{t('CVs')}</th><th>{t('Updated')}</th></tr></thead><tbody>{result.data.items.map(position => <tr key={position.id} className="click-row" onClick={() => window.location.assign(`/positions/${position.id}`)}>{canManage && <td onClick={event => event.stopPropagation()}><input type="checkbox" aria-label={`Select ${position.title}`} checked={selected.includes(position.id)} onChange={event => setSelected(event.target.checked ? [...selected, position.id] : selected.filter(id => id !== position.id))} /></td>}<td><a href={`/positions/${position.id}`}>{position.title}</a></td><td>{position.company || '—'}</td><td>{position.level || '—'}</td><td>{position.cvCount}</td><td>{dateText(position.updatedAt)}</td></tr>)}</tbody></table></div>}<Pager page={page} total={result.data?.totalPages ?? 0} onPage={setPage} /></section>
}
