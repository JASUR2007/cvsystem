import { useEffect, useState, type FormEvent } from 'react'
import { api, json, type AttributeDetail, type AttributeListItem, type Page, type PositionDetail, type PositionLevel } from '../shared/api'
import { PageTitle, Status } from '../shared/ui'
import { useApi } from '../shared/useApi'

type SelectedAttribute = { attributeId: string; name: string; sortOrder: number; isRequired: boolean }
type Rule = { attributeId: string; operator: string; comparisonValue: string }

export default function PositionFormPage({ id }: { id?: string }) {
  const existing = useApi<PositionDetail>(id ? `/positions/${id}` : null)
  const [lookup, setLookup] = useState('')
  const library = useApi<Page<AttributeListItem>>(`/attributes?prefix=${encodeURIComponent(lookup)}&pageSize=30`)
  const [title, setTitle] = useState('')
  const [description, setDescription] = useState('')
  const [company, setCompany] = useState('')
  const [level, setLevel] = useState<PositionLevel | ''>('')
  const [isPublic, setIsPublic] = useState(true)
  const [maxProjects, setMaxProjects] = useState(3)
  const [attributes, setAttributes] = useState<SelectedAttribute[]>([])
  const [rules, setRules] = useState<Rule[]>([])
  const [ruleDefinitions, setRuleDefinitions] = useState<Record<string, AttributeDetail>>({})
  const ruleIds = rules.map(rule => rule.attributeId).filter(Boolean).join(',')
  const [tags, setTags] = useState('')
  const [step, setStep] = useState('General')
  const [message, setMessage] = useState('')
  const [saving, setSaving] = useState(false)

  useEffect(() => {
    if (!id || !existing.data || !('title' in existing.data)) return
    const value = existing.data
    queueMicrotask(() => {
      setTitle(value.title)
      setDescription(value.shortDescription)
      setCompany(value.company ?? '')
      setLevel(value.level ?? '')
      setIsPublic(value.isPublic)
      setMaxProjects(value.maxProjects)
      setAttributes(value.attributes.map(item => ({ attributeId: item.attributeId, name: item.name, sortOrder: item.sortOrder, isRequired: item.isRequired })))
      setRules(value.accessRules.map(item => ({ attributeId: item.attributeId, operator: item.operator, comparisonValue: item.comparisonValue })))
      setTags(value.projectTags.join(', '))
    })
  }, [id, existing.data])

  useEffect(() => {
    const ids = ruleIds.split(',').filter(Boolean)
    if (ids.length === 0) return
    let active = true
    Promise.all(ids.map(id => api<AttributeDetail>(`/attributes/${id}`))).then(definitions => {
      if (active) setRuleDefinitions(Object.fromEntries(definitions.map(definition => [definition.id, definition])))
    }).catch(() => undefined)
    return () => { active = false }
  }, [ruleIds])

  async function save(event: FormEvent) {
    event.preventDefault()
    setSaving(true)
    setMessage('')
    const body = { title, shortDescription: description, company: company || null, level: level || null, isPublic, maxProjects,
      attributes: attributes.map((item, index) => ({ attributeId: item.attributeId, sortOrder: index, isRequired: item.isRequired })),
      accessRules: isPublic ? [] : rules.filter(rule => rule.attributeId && rule.comparisonValue),
      projectTags: tags.split(',').map(tag => tag.trim()).filter(Boolean), version: id && existing.data && 'version' in existing.data ? existing.data.version : 1 }
    try {
      const saved = await api<PositionDetail>(id ? `/positions/${id}` : '/positions', json(id ? 'PUT' : 'POST', body))
      window.location.assign(`/positions/${saved.id}`)
    } catch (cause) {
      setMessage(cause instanceof Error ? cause.message : 'Could not save position.')
    } finally { setSaving(false) }
  }

  if (id && (existing.loading || existing.error)) return <Status loading={existing.loading} error={existing.error} />
  return <section className="surface page-surface"><PageTitle title={id ? 'Edit position' : 'Create position'} /><div className="step-tabs">{['General', 'Attributes', 'Access Rules', 'Projects', 'Preview'].map(item => <button className={step === item ? 'active' : ''} key={item} onClick={() => setStep(item)}>{item}</button>)}</div><form onSubmit={save}>{step === 'General' && <div className="form-grid"><label>Title<input className="form-control" value={title} onChange={event => setTitle(event.target.value)} maxLength={200} required /></label><label>Company<input className="form-control" value={company} onChange={event => setCompany(event.target.value)} maxLength={160} /></label><label>Level<select className="form-select" value={level} onChange={event => setLevel(event.target.value as PositionLevel | '')}><option value="">None</option>{['Junior', 'Middle', 'Senior', 'Lead'].map(item => <option key={item}>{item}</option>)}</select></label><label className="full">Short description<textarea className="form-control" rows={4} value={description} onChange={event => setDescription(event.target.value)} maxLength={2000} /></label></div>}
      {step === 'Attributes' && <><div className="toolbar"><input className="form-control" placeholder="Find attribute by prefix" value={lookup} onChange={event => setLookup(event.target.value)} /><a href="/attributes/new" className="btn btn-outline-primary">New attribute</a></div><div className="choice-list">{library.data?.items.filter(item => !attributes.some(selected => selected.attributeId === item.id)).map(item => <button type="button" className="choice" key={item.id} onClick={() => setAttributes([...attributes, { attributeId: item.id, name: item.name, sortOrder: attributes.length, isRequired: true }])}>+ {item.name} <small>{item.category} · {item.type}</small></button>)}</div><h3>Selected attributes</h3><div className="table-responsive"><table className="table"><thead><tr><th>Attribute</th><th>Required</th><th>Order</th></tr></thead><tbody>{attributes.map((item, index) => <tr key={item.attributeId}><td><button type="button" className="text-link" onClick={() => setAttributes(attributes.filter(value => value.attributeId !== item.attributeId))}>×</button> {item.name}</td><td><input type="checkbox" checked={item.isRequired} onChange={event => setAttributes(attributes.map(value => value.attributeId === item.attributeId ? { ...value, isRequired: event.target.checked } : value))} /></td><td><button type="button" className="btn btn-sm btn-light" disabled={index === 0} onClick={() => { const copy = [...attributes]; [copy[index - 1], copy[index]] = [copy[index], copy[index - 1]]; setAttributes(copy) }}>↑</button> <button type="button" className="btn btn-sm btn-light" disabled={index === attributes.length - 1} onClick={() => { const copy = [...attributes]; [copy[index], copy[index + 1]] = [copy[index + 1], copy[index]]; setAttributes(copy) }}>↓</button></td></tr>)}</tbody></table></div></>}
      {step === 'Access Rules' && <><label className="check-line"><input type="checkbox" checked={isPublic} onChange={event => setIsPublic(event.target.checked)} /> Public position</label>{!isPublic && <><p>Every rule must match a candidate profile before they can create a CV.</p>{rules.map((rule, index) => { const definition = ruleDefinitions[rule.attributeId]; const type = definition?.type; const operators = type === 'Numeric' ? ['Equals', 'GreaterThan', 'GreaterThanOrEqual', 'LessThan', 'LessThanOrEqual'] : type === 'String' || type === 'Text' ? ['Equals', 'Contains'] : ['Equals']; const update = (change: Partial<Rule>) => setRules(rules.map((item, position) => position === index ? { ...item, ...change } : item)); return <div className="rule-row" key={index}><select className="form-select" value={rule.attributeId} onChange={event => update({ attributeId: event.target.value, operator: 'Equals', comparisonValue: '' })}><option value="">Attribute</option>{library.data?.items.filter(item => !item.isBuiltIn).map(item => <option value={item.id} key={item.id}>{item.name}</option>)}{attributes.filter(item => !library.data?.items.some(entry => entry.id === item.attributeId)).map(item => <option value={item.attributeId} key={item.attributeId}>{item.name}</option>)}</select><select className="form-select" value={rule.operator} onChange={event => update({ operator: event.target.value })}>{operators.map(item => <option key={item}>{item}</option>)}</select>{type === 'Dropdown' ? <select className="form-select" value={rule.comparisonValue} onChange={event => update({ comparisonValue: event.target.value })}><option value="">Select option</option>{definition.options.map(option => <option key={option.id} value={option.id}>{option.value}</option>)}</select> : type === 'Boolean' ? <select className="form-select" value={rule.comparisonValue} onChange={event => update({ comparisonValue: event.target.value })}><option value="">Select value</option><option value="true">Yes</option><option value="false">No</option></select> : <input className="form-control" type={type === 'Numeric' ? 'number' : 'text'} step={type === 'Numeric' ? 'any' : undefined} value={rule.comparisonValue} onChange={event => update({ comparisonValue: event.target.value })} placeholder="Value" />}<button type="button" className="btn btn-outline-danger" onClick={() => setRules(rules.filter((_, position) => position !== index))}>×</button></div> })}<button type="button" className="btn btn-outline-primary btn-sm" onClick={() => setRules([...rules, { attributeId: '', operator: 'Equals', comparisonValue: '' }])}>+ Add rule</button></>}</>}
      {step === 'Projects' && <div className="form-grid"><label>Technology tags, separated by commas<input className="form-control" value={tags} onChange={event => setTags(event.target.value)} placeholder=".NET, Docker" /></label><label>Maximum projects<input className="form-control" type="number" min={0} max={20} value={maxProjects} onChange={event => setMaxProjects(Number(event.target.value))} /></label></div>}
      {step === 'Preview' && <div className="preview"><h2>{title || 'Position title'}</h2><p>{description || 'Short description'}</p><h3>CV information</h3><ul>{attributes.map(item => <li key={item.attributeId}>{item.name}{item.isRequired ? ' *' : ''}</li>)}</ul><h3>Projects</h3><p>Up to {maxProjects} projects {tags && `matching ${tags}`}</p></div>}
      {message && <div className="alert alert-danger mt-3" role="alert">{message}</div>}<div className="form-actions"><a href={id ? `/positions/${id}` : '/positions'} className="btn btn-outline-secondary">Cancel</a><button className="btn btn-primary" disabled={saving}>{saving ? 'Saving...' : 'Save position'}</button></div></form></section>
}
