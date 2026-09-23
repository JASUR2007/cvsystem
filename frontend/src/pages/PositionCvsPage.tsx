import { useState } from 'react'
import type { PositionCvPage } from '../shared/api'
import { PageTitle, Pager, Status } from '../shared/ui'
import { useApi } from '../shared/useApi'

export default function PositionCvsPage({ id }: { id: string }) {
  const [query, setQuery] = useState('')
  const [attributeId, setAttributeId] = useState('')
  const [operation, setOperation] = useState('Equals')
  const [value, setValue] = useState('')
  const [sort, setSort] = useState('newest')
  const [page, setPage] = useState(1)
  const filter = attributeId && value ? `&attributeId=${attributeId}&operation=${operation}&value=${encodeURIComponent(value)}` : ''
  const result = useApi<PositionCvPage>(`/positions/${id}/cvs?q=${encodeURIComponent(query)}${filter}&sort=${sort}&page=${page}`)

  return <section className="surface page-surface"><a className="back-link" href={`/positions/${id}`}>← Position</a><PageTitle title="Position CVs" /><div className="toolbar"><input className="form-control" placeholder="Search candidate" value={query} onChange={event => { setQuery(event.target.value); setPage(1) }} /><select className="form-select" value={attributeId} onChange={event => { setAttributeId(event.target.value); setPage(1) }}><option value="">All attributes</option>{result.data?.columns.map(column => <option value={column.attributeId} key={column.attributeId}>{column.name}</option>)}</select><select className="form-select" value={operation} onChange={event => setOperation(event.target.value)}>{['Equals','GreaterThan','GreaterThanOrEqual','LessThan','LessThanOrEqual','Contains'].map(item => <option key={item}>{item}</option>)}</select><input className="form-control" placeholder="Filter value" value={value} onChange={event => { setValue(event.target.value); setPage(1) }} /><select className="form-select" value={sort} onChange={event => setSort(event.target.value)}><option value="newest">Newest</option><option value="likes">Most liked</option><option value="candidate">Candidate</option></select></div><Status loading={result.loading} error={result.error} empty={result.data?.items.length === 0} />{result.data && result.data.items.length > 0 && <div className="table-responsive"><table className="table table-hover align-middle"><thead><tr><th>Candidate</th>{result.data.columns.map(column => <th key={column.attributeId}>{column.name}</th>)}<th>Likes</th></tr></thead><tbody>{result.data.items.map(cv => <tr key={cv.id} className="click-row" onClick={() => window.location.assign(`/cvs/${cv.id}`)}><td><a href={`/cvs/${cv.id}`}>{cv.candidate}</a></td>{result.data!.columns.map(column => <td key={column.attributeId} className={!cv.values.find(item => item.attributeId === column.attributeId)?.value ? 'missing-value' : ''}>{cv.values.find(item => item.attributeId === column.attributeId)?.value || '⚠ Empty'}</td>)}<td>♥ {cv.likes}</td></tr>)}</tbody></table></div>}<Pager page={page} total={Math.ceil((result.data?.totalItems ?? 0) / (result.data?.pageSize ?? 20))} onPage={setPage} /></section>
}
