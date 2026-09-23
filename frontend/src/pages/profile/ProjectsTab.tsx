import { useState, type FormEvent } from 'react'
import Markdown from 'react-markdown'
import { api, json, type Project } from '../../shared/api'
import { Status } from '../../shared/ui'
import { useApi } from '../../shared/useApi'

type Form = { id?: string; name: string; startedOn: string; endedOn: string; description: string; tags: string; version: number }
const blank: Form = { name: '', startedOn: '', endedOn: '', description: '', tags: '', version: 1 }

export default function ProjectsTab({ userId }: { userId?: string }) {
  const result = useApi<Project[]>(`/projects${userId ? `?userId=${userId}` : ''}`)
  const [form, setForm] = useState<Form | null>(null)
  const [selected, setSelected] = useState<string[]>([])
  const [message, setMessage] = useState('')
  const prefix = form?.tags.split(',').at(-1)?.trim() ?? ''
  const tags = useApi<string[]>(form ? `/tags?prefix=${encodeURIComponent(prefix)}` : null)

  async function save(event: FormEvent) {
    event.preventDefault()
    if (!form) return
    try {
      const body = { name: form.name, startedOn: form.startedOn, endedOn: form.endedOn || null, description: form.description,
        tags: form.tags.split(',').map(tag => tag.trim()).filter(Boolean), version: form.version }
      await api(form.id ? `/projects/${form.id}` : `/projects${userId ? `?userId=${userId}` : ''}`, json(form.id ? 'PUT' : 'POST', body))
      setForm(null)
      result.reload()
      setMessage('Project saved.')
    } catch (cause) { setMessage(cause instanceof Error ? cause.message : 'Could not save project.') }
  }

  async function remove() {
    if (!window.confirm(`Delete ${selected.length} selected project(s)?`)) return
    try { await Promise.all(selected.map(id => api(`/projects/${id}`, { method: 'DELETE' }))); setSelected([]); result.reload(); setMessage('Projects deleted.') }
    catch (cause) { setMessage(cause instanceof Error ? cause.message : 'Could not delete projects.') }
  }

  return <><div className="toolbar"><button className="btn btn-primary" onClick={() => setForm(blank)}>+ Add project</button><button className="btn btn-outline-danger" disabled={selected.length === 0} onClick={remove}>Delete selected</button></div>{message && <div className="alert alert-info">{message}</div>}<Status loading={result.loading} error={result.error} empty={result.data?.length === 0} />{result.data && <div className="table-responsive"><table className="table table-hover"><thead><tr><th></th><th>Name</th><th>Period</th><th>Tags</th></tr></thead><tbody>{result.data.map(project => <tr key={project.id} className="click-row" onClick={() => setForm({ id: project.id, name: project.name, startedOn: project.startedOn, endedOn: project.endedOn ?? '', description: project.description, tags: project.tags.join(', '), version: project.version })}><td onClick={event => event.stopPropagation()}><input type="checkbox" aria-label={`Select ${project.name}`} checked={selected.includes(project.id)} onChange={event => setSelected(event.target.checked ? [...selected, project.id] : selected.filter(id => id !== project.id))} /></td><td>{project.name}</td><td>{project.startedOn} – {project.endedOn || 'Present'}</td><td>{project.tags.join(', ')}</td></tr>)}</tbody></table></div>}{form && <form className="edit-panel stack-form" onSubmit={save}><h3>{form.id ? 'Edit project' : 'Add project'}</h3><label>Name<input className="form-control" value={form.name} onChange={event => setForm({ ...form, name: event.target.value })} required maxLength={200} /></label><div className="form-grid"><label>Start<input type="date" className="form-control" value={form.startedOn} onChange={event => setForm({ ...form, startedOn: event.target.value })} required /></label><label>End<input type="date" className="form-control" value={form.endedOn} onChange={event => setForm({ ...form, endedOn: event.target.value })} /></label></div><label>Technology tags, separated by commas<input className="form-control" list="tag-suggestions" value={form.tags} onChange={event => setForm({ ...form, tags: event.target.value })} /><datalist id="tag-suggestions">{tags.data?.map(tag => <option value={tag} key={tag} />)}</datalist></label><label>Description (Markdown)<textarea className="form-control" rows={5} value={form.description} onChange={event => setForm({ ...form, description: event.target.value })} /></label>{form.description && <div className="preview"><Markdown>{form.description}</Markdown></div>}<div className="form-actions"><button type="button" className="btn btn-outline-secondary" onClick={() => setForm(null)}>Cancel</button><button className="btn btn-primary">Save project</button></div></form>}</>
}
