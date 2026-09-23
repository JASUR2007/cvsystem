import { useState } from 'react'
import { dateText, type Page, type PositionListItem } from '../shared/api'
import { PageTitle, Status } from '../shared/ui'
import { useApi } from '../shared/useApi'
import type { CurrentUser } from '../auth/api'

type SearchCv = { id: string; positionId: string; position: string; candidate: string; updatedAt: string; likes: number }

export default function SearchPage({ user }: { user: CurrentUser | null }) {
  const initial = new URLSearchParams(window.location.search).get('q') ?? ''
  const [query, setQuery] = useState(initial)
  const [term, setTerm] = useState(initial)
  const positions = useApi<Page<PositionListItem>>(`/search/positions?q=${encodeURIComponent(term)}`)
  const canSeeCvs = user?.roles.some(role => role === 'Recruiter' || role === 'Administrator') ?? false
  const cvs = useApi<Page<SearchCv>>(canSeeCvs ? `/search/cvs?q=${encodeURIComponent(term)}` : null)

  return <section className="surface page-surface"><PageTitle title="Search" /><form className="toolbar" onSubmit={event => { event.preventDefault(); setTerm(query); history.replaceState(null, '', `/search?q=${encodeURIComponent(query)}`) }}><input className="form-control" value={query} onChange={event => setQuery(event.target.value)} placeholder="Search positions and CVs" /><button className="btn btn-primary">Search</button></form><h2>Positions</h2><Status loading={positions.loading} error={positions.error} empty={positions.data?.items.length === 0} />{positions.data && positions.data.items.length > 0 && <div className="table-responsive"><table className="table table-hover"><thead><tr><th>Title</th><th>Company</th><th>Level</th><th>Updated</th></tr></thead><tbody>{positions.data.items.map(position => <tr key={position.id} className="click-row" onClick={() => window.location.assign(`/positions/${position.id}`)}><td><a href={`/positions/${position.id}`}>{position.title}</a></td><td>{position.company || '—'}</td><td>{position.level || '—'}</td><td>{dateText(position.updatedAt)}</td></tr>)}</tbody></table></div>}{canSeeCvs && <><h2 className="mt-4">Published CVs</h2><Status loading={cvs.loading} error={cvs.error} empty={cvs.data?.items.length === 0} />{cvs.data && cvs.data.items.length > 0 && <div className="table-responsive"><table className="table table-hover"><thead><tr><th>Candidate</th><th>Position</th><th>Likes</th><th>Updated</th></tr></thead><tbody>{cvs.data.items.map(cv => <tr key={cv.id} className="click-row" onClick={() => window.location.assign(`/cvs/${cv.id}`)}><td><a href={`/cvs/${cv.id}`}>{cv.candidate}</a></td><td>{cv.position}</td><td>{cv.likes}</td><td>{dateText(cv.updatedAt)}</td></tr>)}</tbody></table></div>}</>}</section>
}
