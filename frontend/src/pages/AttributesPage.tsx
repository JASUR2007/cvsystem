import { useState } from 'react'
import { api, type AttributeListItem, type Page } from '../shared/api'
import { PageTitle, Pager, Status } from '../shared/ui'
import { useApi } from '../shared/useApi'

export default function AttributesPage() {
  const [prefix, setPrefix] = useState('')
  const [category, setCategory] = useState('')
  const [type, setType] = useState('')
  const [recent, setRecent] = useState(false)
  const [page, setPage] = useState(1)
  const [selected, setSelected] = useState<string[]>([])
  const [message, setMessage] = useState('')
  const categories = useApi<string[]>('/attribute-categories')
  const result = useApi<Page<AttributeListItem>>(`/attributes?prefix=${encodeURIComponent(prefix)}&category=${encodeURIComponent(category)}&type=${type}&recent=${recent}&page=${page}`)

  async function remove() {
    if (!window.confirm(`Delete ${selected.length} selected attribute(s)?`)) return
    try {
      await Promise.all(selected.map(id => api(`/attributes/${id}`, { method: 'DELETE' })))
      setSelected([])
      result.reload()
      setMessage('Attributes deleted.')
    } catch (cause) { setMessage(cause instanceof Error ? cause.message : 'Could not delete attributes.') }
  }

  return <section className="surface page-surface"><PageTitle title="Attribute library" action={<a href="/attributes/new" className="btn btn-primary btn-sm">+ New attribute</a>} /><div className="toolbar"><input className="form-control" value={prefix} onChange={event => { setPrefix(event.target.value); setPage(1) }} placeholder="Search by prefix" /><select className="form-select" value={category} onChange={event => { setCategory(event.target.value); setPage(1) }}><option value="">All categories</option>{categories.data?.map(item => <option key={item}>{item}</option>)}</select><select className="form-select" value={type} onChange={event => { setType(event.target.value); setPage(1) }}><option value="">All types</option>{['String','Text','Image','Numeric','Date','Period','Boolean','Dropdown'].map(item => <option key={item}>{item}</option>)}</select><label className="check-line"><input type="checkbox" checked={recent} onChange={event => setRecent(event.target.checked)} /> Recent</label><button className="btn btn-outline-danger" disabled={selected.length === 0} onClick={remove}>Delete selected</button></div>{message && <div className="alert alert-info">{message}</div>}<Status loading={result.loading} error={result.error} empty={result.data?.items.length === 0} />{result.data && result.data.items.length > 0 && <div className="table-responsive"><table className="table table-hover align-middle"><thead><tr><th><input type="checkbox" aria-label="Select all" checked={selected.length === result.data.items.filter(item => !item.isBuiltIn).length} onChange={event => setSelected(event.target.checked ? result.data!.items.filter(item => !item.isBuiltIn).map(item => item.id) : [])} /></th><th>Name</th><th>Category</th><th>Type</th><th>Usage</th></tr></thead><tbody>{result.data.items.map(item => <tr key={item.id} className="click-row" onClick={() => window.location.assign(`/attributes/${item.id}/edit`)}><td onClick={event => event.stopPropagation()}>{!item.isBuiltIn && <input type="checkbox" aria-label={`Select ${item.name}`} checked={selected.includes(item.id)} onChange={event => setSelected(event.target.checked ? [...selected, item.id] : selected.filter(id => id !== item.id))} />}</td><td><a href={`/attributes/${item.id}/edit`}>{item.name}</a>{item.isBuiltIn && <span className="badge text-bg-secondary ms-2">Built-in</span>}</td><td>{item.category}</td><td>{item.type}</td><td>{item.usageCount}</td></tr>)}</tbody></table></div>}<Pager page={page} total={result.data?.totalPages ?? 0} onPage={setPage} /></section>
}
