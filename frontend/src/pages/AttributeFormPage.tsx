import { useEffect, useState, type FormEvent } from 'react'
import { api, json, type AttributeDetail, type AttributeType } from '../shared/api'
import { PageTitle, Status } from '../shared/ui'
import { useApi } from '../shared/useApi'

export default function AttributeFormPage({ id }: { id?: string }) {
  const existing = useApi<AttributeDetail>(id ? `/attributes/${id}` : null)
  const categories = useApi<string[]>('/attribute-categories')
  const [name, setName] = useState('')
  const [description, setDescription] = useState('')
  const [category, setCategory] = useState('Technical Skills')
  const [type, setType] = useState<AttributeType>('String')
  const [options, setOptions] = useState<string[]>([''])
  const [message, setMessage] = useState('')
  const [saving, setSaving] = useState(false)

  useEffect(() => {
    if (!existing.data) return
    const value = existing.data
    queueMicrotask(() => {
      setName(value.name)
      setDescription(value.description)
      setCategory(value.category)
      setType(value.type)
      setOptions(value.options.length ? value.options.map(option => option.value) : [''])
    })
  }, [existing.data])

  async function save(event: FormEvent) {
    event.preventDefault()
    setSaving(true)
    setMessage('')
    try {
      await api(id ? `/attributes/${id}` : '/attributes', json(id ? 'PUT' : 'POST', { name, description, category, type, options: type === 'Dropdown' ? options.map(option => option.trim()).filter(Boolean) : [], version: existing.data?.version ?? 1 }))
      window.location.assign('/attributes')
    } catch (cause) { setMessage(cause instanceof Error ? cause.message : 'Could not save attribute.') }
    finally { setSaving(false) }
  }

  if (id && (existing.loading || existing.error)) return <Status loading={existing.loading} error={existing.error} />
  return <section className="surface page-surface narrow"><PageTitle title={id ? 'Edit attribute' : 'Create attribute'} />{existing.data?.isBuiltIn && <div className="alert alert-info">Built-in attributes cannot be edited.</div>}<form onSubmit={save} className="stack-form"><label>Name<input className="form-control" value={name} onChange={event => setName(event.target.value)} maxLength={160} required disabled={existing.data?.isBuiltIn} /></label><label>Description<textarea className="form-control" value={description} onChange={event => setDescription(event.target.value)} rows={3} maxLength={2000} disabled={existing.data?.isBuiltIn} /></label><label>Category<select className="form-select" value={category} onChange={event => setCategory(event.target.value)} disabled={existing.data?.isBuiltIn}>{categories.data?.map(item => <option key={item}>{item}</option>)}</select></label><label>Type<select className="form-select" value={type} onChange={event => setType(event.target.value as AttributeType)} disabled={existing.data?.isBuiltIn}>{['String','Text','Image','Numeric','Date','Period','Boolean','Dropdown'].map(item => <option key={item}>{item}</option>)}</select></label>{type === 'Dropdown' && <div><h3>Options</h3>{options.map((option, index) => <div className="option-row" key={index}><input className="form-control" value={option} onChange={event => setOptions(options.map((item, position) => position === index ? event.target.value : item))} placeholder={`Option ${index + 1}`} /><button type="button" className="btn btn-outline-danger" onClick={() => setOptions(options.filter((_, position) => position !== index))}>×</button></div>)}<button type="button" className="btn btn-outline-primary btn-sm" onClick={() => setOptions([...options, ''])}>+ Add option</button></div>}{message && <div className="alert alert-danger">{message}</div>}<div className="form-actions"><a href="/attributes" className="btn btn-outline-secondary">Cancel</a><button className="btn btn-primary" disabled={saving || existing.data?.isBuiltIn}>{saving ? 'Saving...' : 'Save attribute'}</button></div></form></section>
}
