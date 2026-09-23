import { useState } from 'react'
import { api, json, type AttributeDetail, type AttributeListItem, type AttributeValue, type Page } from '../../shared/api'
import AttributeEditor from '../../shared/AttributeEditor'
import { Status } from '../../shared/ui'
import { useApi } from '../../shared/useApi'

export default function InfoTab({ userId }: { userId?: string }) {
  const target = userId ? `?userId=${userId}` : ''
  const result = useApi<AttributeValue[]>(`/profile/attributes${target}`)
  const [lookup, setLookup] = useState('')
  const library = useApi<Page<AttributeListItem>>(`/attributes?prefix=${encodeURIComponent(lookup)}&pageSize=20`)
  const [selected, setSelected] = useState<string[]>([])
  const [editing, setEditing] = useState<AttributeValue | null>(null)
  const definition = useApi<AttributeDetail>(editing ? `/attributes/${editing.attributeId}` : null)
  const [message, setMessage] = useState('')

  async function add(id: string) {
    try { await api(`/profile/attributes/${id}${target}`, { method: 'POST' }); result.reload(); setMessage('Attribute added.') }
    catch (cause) { setMessage(cause instanceof Error ? cause.message : 'Could not add attribute.') }
  }

  async function save() {
    if (!editing) return
    try {
      await api(`/profile/attributes/${editing.attributeId}${target}`, json('PUT', { version: editing.version, value: editing }))
      setEditing(null)
      result.reload()
      setMessage('Value saved.')
    } catch (cause) { setMessage(cause instanceof Error ? cause.message : 'Could not save value.') }
  }

  async function remove() {
    if (!window.confirm(`Remove ${selected.length} selected attribute(s)?`)) return
    try { await Promise.all(selected.map(id => api(`/profile/attributes/${id}${target}`, { method: 'DELETE' }))); setSelected([]); result.reload(); setMessage('Attributes removed.') }
    catch (cause) { setMessage(cause instanceof Error ? cause.message : 'Could not remove attributes.') }
  }

  return <><div className="toolbar"><input className="form-control" placeholder="Find an attribute to add" value={lookup} onChange={event => setLookup(event.target.value)} /><button className="btn btn-outline-danger" disabled={selected.length === 0} onClick={remove}>Remove selected</button></div><div className="choice-list">{library.data?.items.filter(item => !item.isBuiltIn && !result.data?.some(value => value.attributeId === item.id)).slice(0, 8).map(item => <button type="button" className="choice" key={item.id} onClick={() => add(item.id)}>+ {item.name} <small>{item.category}</small></button>)}</div>{message && <div className="alert alert-info">{message}</div>}<Status loading={result.loading} error={result.error} empty={result.data?.length === 0} />{result.data && <div className="table-responsive"><table className="table table-hover align-middle"><thead><tr><th></th><th>Attribute</th><th>Category</th><th>Value</th></tr></thead><tbody>{result.data.map(value => <tr key={value.attributeId} className="click-row" onClick={() => setEditing(value)}><td onClick={event => event.stopPropagation()}>{!['First Name','Last Name','Location','Personal Photo'].includes(value.name) && <input type="checkbox" checked={selected.includes(value.attributeId)} onChange={event => setSelected(event.target.checked ? [...selected, value.attributeId] : selected.filter(id => id !== value.attributeId))} aria-label={`Select ${value.name}`} />}</td><td>{value.name}</td><td>{value.category}</td><td>{value.textValue ?? value.numberValue ?? value.dateValue ?? value.booleanValue?.toString() ?? (value.selectedOptionId ? 'Selected' : value.imageObjectKey ? 'Image' : 'Empty')}</td></tr>)}</tbody></table></div>}{editing && <div className="edit-panel"><h3>Edit {editing.name}</h3>{['First Name','Last Name','Location','Personal Photo'].includes(editing.name) ? <p>Edit this field on the Me tab.</p> : <><Status loading={definition.loading} error={definition.error} /><AttributeEditor value={editing} options={definition.data?.options} onChange={setEditing} userId={userId} /><div className="form-actions"><button className="btn btn-outline-secondary" onClick={() => setEditing(null)}>Cancel</button><button className="btn btn-primary" onClick={save}>Save</button></div></>}</div>}</>
}
