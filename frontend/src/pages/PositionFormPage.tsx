import { useEffect, useState, type FormEvent } from 'react'
import {
  api,
  json,
  type AttributeDetail,
  type AttributeListItem,
  type AttributeType,
  type Page,
  type PositionDetail,
  type PositionLevel,
} from '../shared/api'
import { Status } from '../shared/ui'
import { useApi } from '../shared/useApi'
import { t } from '../shared/i18n'
import '../position-form.css'

type SelectedAttribute = {
  attributeId: string
  name: string
  sortOrder: number
  isRequired: boolean
}

type Rule = {
  attributeId: string
  operator: string
  comparisonValue: string
}

const STEPS = [
  { id: 'General', label: '1 General' },
  { id: 'Attributes', label: '2 Attributes' },
  { id: 'Access Rules', label: '3 Access Rules' },
  { id: 'Projects', label: '4 Projects' },
  { id: 'Preview', label: '5 Preview' },
] as const

type StepId = (typeof STEPS)[number]['id']

export default function PositionFormPage({ id }: { id?: string }) {
  const existing = useApi<PositionDetail>(id ? `/positions/${id}` : null)
  const [lookup, setLookup] = useState('')
  const library = useApi<Page<AttributeListItem>>(`/attributes?prefix=${encodeURIComponent(lookup)}&pageSize=40`)

  const [title, setTitle] = useState('')
  const [description, setDescription] = useState('')
  const [company, setCompany] = useState('')
  const [level, setLevel] = useState<PositionLevel | ''>('Middle')
  const [isPublic, setIsPublic] = useState(true)
  const [maxProjects, setMaxProjects] = useState(3)
  const [attributes, setAttributes] = useState<SelectedAttribute[]>([])
  const [rules, setRules] = useState<Rule[]>([])
  const [ruleDefinitions, setRuleDefinitions] = useState<Record<string, AttributeDetail>>({})
  const [tags, setTags] = useState('')
  const [step, setStep] = useState<StepId>('General')
  const [message, setMessage] = useState('')
  const [saving, setSaving] = useState(false)

  // In-wizard Attribute Creation Modal State
  const categoriesApi = useApi<string[]>('/attribute-categories')
  const DEFAULT_CATEGORIES = [
    'Technical Skills',
    'Soft Skills',
    'Certificates',
    'Language',
    'Education',
    'Domain Knowledge',
    'Personal Information',
    'General',
  ]
  const availableCategories =
    categoriesApi.data && categoriesApi.data.length > 0
      ? Array.from(new Set([...categoriesApi.data, ...DEFAULT_CATEGORIES]))
      : DEFAULT_CATEGORIES

  const [showNewAttrModal, setShowNewAttrModal] = useState(false)
  const [newAttrName, setNewAttrName] = useState('')
  const [newAttrCategory, setNewAttrCategory] = useState('Technical Skills')
  const [newAttrType, setNewAttrType] = useState<AttributeType>('String')
  const [newAttrDesc, setNewAttrDesc] = useState('')
  const [newAttrOptions, setNewAttrOptions] = useState<string[]>([''])
  const [newAttrError, setNewAttrError] = useState('')
  const [creatingAttr, setCreatingAttr] = useState(false)

  async function handleCreateNewAttribute(e: FormEvent) {
    e.preventDefault()
    setNewAttrError('')
    if (!newAttrName.trim()) {
      setNewAttrError(t('Name is required'))
      return
    }
    setCreatingAttr(true)
    try {
      const created = await api<AttributeDetail>(
        '/attributes',
        json('POST', {
          name: newAttrName.trim(),
          description: newAttrDesc.trim(),
          category: newAttrCategory,
          type: newAttrType,
          options: newAttrType === 'Dropdown' ? newAttrOptions.map(o => o.trim()).filter(Boolean) : [],
          version: 1,
        })
      )
      await library.reload()
      // Automatically add to position attributes
      setAttributes(prev => [
        ...prev,
        {
          attributeId: created.id,
          name: created.name,
          sortOrder: prev.length,
          isRequired: true,
        },
      ])
      setShowNewAttrModal(false)
      setNewAttrName('')
      setNewAttrDesc('')
      setNewAttrType('String')
      setNewAttrOptions([''])
    } catch (cause) {
      setNewAttrError(cause instanceof Error ? cause.message : t('Could not create attribute.'))
    } finally {
      setCreatingAttr(false)
    }
  }

  const ruleIds = rules.map(rule => rule.attributeId).filter(Boolean).join(',')

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
      setAttributes(
        value.attributes.map(item => ({
          attributeId: item.attributeId,
          name: item.name,
          sortOrder: item.sortOrder,
          isRequired: item.isRequired,
        }))
      )
      setRules(
        value.accessRules.map(item => ({
          attributeId: item.attributeId,
          operator: item.operator,
          comparisonValue: item.comparisonValue,
        }))
      )
      setTags(value.projectTags.join(', '))
    })
  }, [id, existing.data])

  useEffect(() => {
    const ids = ruleIds.split(',').filter(Boolean)
    if (ids.length === 0) return
    let active = true
    Promise.all(ids.map(id => api<AttributeDetail>(`/attributes/${id}`)))
      .then(definitions => {
        if (active) setRuleDefinitions(Object.fromEntries(definitions.map(def => [def.id, def])))
      })
      .catch(() => undefined)
    return () => {
      active = false
    }
  }, [ruleIds])

  async function save(event: FormEvent) {
    event.preventDefault()
    setSaving(true)
    setMessage('')
    const body = {
      title,
      shortDescription: description,
      company: company || null,
      level: level || null,
      isPublic,
      maxProjects,
      attributes: attributes.map((item, index) => ({
        attributeId: item.attributeId,
        sortOrder: index,
        isRequired: item.isRequired,
      })),
      accessRules: isPublic ? [] : rules.filter(rule => rule.attributeId && rule.comparisonValue),
      projectTags: tags.split(',').map(tag => tag.trim()).filter(Boolean),
      version: id && existing.data && 'version' in existing.data ? existing.data.version : 1,
    }

    try {
      const saved = await api<PositionDetail>(id ? `/positions/${id}` : '/positions', json(id ? 'PUT' : 'POST', body))
      window.location.assign(`/positions/${saved.id}`)
    } catch (cause) {
      setMessage(cause instanceof Error ? cause.message : 'Could not save position.')
    } finally {
      setSaving(false)
    }
  }

  function nextStep() {
    const currentIndex = STEPS.findIndex(s => s.id === step)
    if (currentIndex < STEPS.length - 1) {
      setStep(STEPS[currentIndex + 1].id)
    }
  }

  function prevStep() {
    const currentIndex = STEPS.findIndex(s => s.id === step)
    if (currentIndex > 0) {
      setStep(STEPS[currentIndex - 1].id)
    }
  }

  if (id && (existing.loading || existing.error)) {
    return (
      <div className="pos-form-container">
        <Status loading={existing.loading} error={existing.error} />
      </div>
    )
  }

  return (
    <div className="pos-form-container">
      {/* Navigation */}
      <a className="back-nav-link" href={id ? `/positions/${id}` : '/positions'}>
        <svg width="16" height="16" viewBox="0 0 24 24" fill="none" stroke="currentColor" strokeWidth="2">
          <line x1="19" y1="12" x2="5" y2="12" />
          <polyline points="12 19 5 12 12 5" />
        </svg>
        <span>{t('Back to positions')}</span>
      </a>

      <div className="pos-form-card">
        <header className="pos-form-header">
          <h1 className="pos-form-title">{id ? t('Edit position') : t('Create position')}</h1>
          <p className="text-muted mb-0">
            {t('Define requirements, access criteria, and CV template structure for candidates.')}
          </p>
        </header>

        {/* 5-Step Stepper Header */}
        <nav className="stepper-nav" aria-label="Creation Steps">
          {STEPS.map((s, idx) => (
            <button
              type="button"
              key={s.id}
              className={`stepper-step ${step === s.id ? 'active' : ''}`}
              onClick={() => setStep(s.id)}
            >
              <span className="step-num-circle">{idx + 1}</span>
              <span>{t(s.id)}</span>
            </button>
          ))}
        </nav>

        {message && (
          <div className="alert alert-danger mb-4" role="alert">
            {message}
          </div>
        )}

        <form onSubmit={save}>
          {/* STEP 1: GENERAL */}
          {step === 'General' && (
            <div className="form-step-content">
              <div className="form-field-group">
                <label>{t('Title')} *</label>
                <input
                  className="form-control"
                  value={title}
                  onChange={e => setTitle(e.target.value)}
                  placeholder="e.g. Backend Developer"
                  maxLength={200}
                  required
                />
              </div>

              <div className="form-field-group">
                <label>{t('Description')} *</label>
                <textarea
                  className="form-control"
                  rows={4}
                  value={description}
                  onChange={e => setDescription(e.target.value)}
                  placeholder="We are looking for a skilled developer to join our team..."
                  maxLength={2000}
                  required
                />
              </div>

              <div className="row g-3">
                <div className="col-md-6 form-field-group">
                  <label>{t('Level')}</label>
                  <select
                    className="form-select"
                    value={level}
                    onChange={e => setLevel(e.target.value as PositionLevel | '')}
                  >
                    <option value="">{t('Any level')}</option>
                    {['Junior', 'Middle', 'Senior', 'Lead'].map(item => (
                      <option key={item} value={item}>{item}</option>
                    ))}
                  </select>
                </div>

                <div className="col-md-6 form-field-group">
                  <label>{t('Company')} (optional)</label>
                  <input
                    className="form-control"
                    value={company}
                    onChange={e => setCompany(e.target.value)}
                    placeholder="e.g. TechCorp"
                    maxLength={160}
                  />
                </div>
              </div>

              <div className="form-field-group">
                <label>{t('Access')}</label>
                <div className="visibility-options">
                  <label className={`visibility-card ${isPublic ? 'selected' : ''}`}>
                    <input
                      type="radio"
                      name="visibility"
                      checked={isPublic}
                      onChange={() => setIsPublic(true)}
                    />
                    <div className="visibility-card-body">
                      <strong>{t('Public (visible to all candidates)')}</strong>
                      <span>{t('Any registered user can evaluate and submit a CV.')}</span>
                    </div>
                  </label>

                  <label className={`visibility-card ${!isPublic ? 'selected' : ''}`}>
                    <input
                      type="radio"
                      name="visibility"
                      checked={!isPublic}
                      onChange={() => setIsPublic(false)}
                    />
                    <div className="visibility-card-body">
                      <strong>{t('Restricted (filtered access)')}</strong>
                      <span>{t('Only candidates matching specific attribute rules can create CVs.')}</span>
                    </div>
                  </label>
                </div>
              </div>
            </div>
          )}

          {/* STEP 2: ATTRIBUTES */}
          {step === 'Attributes' && (
            <div className="form-step-content">
              <div className="pos-attr-picker-bar">
                <input
                  className="form-control"
                  placeholder={t('Search by prefix')}
                  value={lookup}
                  onChange={e => setLookup(e.target.value)}
                />
                <button
                  type="button"
                  className="btn btn-outline-primary fw-bold px-3 d-inline-flex align-items-center justify-content-center"
                  style={{ fontSize: '1.25rem', lineHeight: 1, minWidth: '44px' }}
                  title={t('New attribute')}
                  onClick={() => setShowNewAttrModal(true)}
                >
                  +
                </button>
              </div>

              <label className="text-muted" style={{ fontSize: '0.8125rem' }}>
                {t('Click to add attributes from the library to this CV template:')}
              </label>

              <div className="pos-attr-available-list">
                {library.data?.items
                  .filter(item => !attributes.some(sel => sel.attributeId === item.id))
                  .map(item => (
                    <button
                      type="button"
                      className="pos-add-attr-chip"
                      key={item.id}
                      onClick={() =>
                        setAttributes([
                          ...attributes,
                          {
                            attributeId: item.id,
                            name: item.name,
                            sortOrder: attributes.length,
                            isRequired: true,
                          },
                        ])
                      }
                    >
                      <span>+ {item.name}</span>
                      <small className="text-muted">({item.category})</small>
                    </button>
                  ))}
              </div>

              <h3 style={{ fontSize: '1.05rem', fontWeight: 600 }}>{t('Selected Attributes')} ({attributes.length})</h3>

              {attributes.length === 0 ? (
                <p className="text-muted">{t('No extra attributes')}</p>
              ) : (
                <div className="table-responsive">
                  <table className="pos-selected-attrs-table">
                    <thead>
                      <tr>
                        <th>{t('Attribute')}</th>
                        <th style={{ width: 100 }}>{t('Required')}</th>
                        <th style={{ width: 90, textAlign: 'right' }}>{t('Order')}</th>
                      </tr>
                    </thead>
                    <tbody>
                      {attributes.map((item, index) => (
                        <tr key={item.attributeId}>
                          <td>
                            <button
                              type="button"
                              className="btn btn-sm btn-link text-danger p-0 me-2"
                              onClick={() => setAttributes(attributes.filter(a => a.attributeId !== item.attributeId))}
                            >
                              ✕
                            </button>
                            <strong>{item.name}</strong>
                          </td>
                          <td>
                            <input
                              type="checkbox"
                              className="form-check-input"
                              checked={item.isRequired}
                              onChange={e =>
                                setAttributes(
                                  attributes.map(a =>
                                    a.attributeId === item.attributeId ? { ...a, isRequired: e.target.checked } : a
                                  )
                                )
                              }
                            />
                          </td>
                          <td style={{ textAlign: 'right' }}>
                            <div className="btn-group btn-group-sm">
                              <button
                                type="button"
                                className="btn btn-outline-secondary btn-sm"
                                disabled={index === 0}
                                onClick={() => {
                                  const copy = [...attributes]
                                  ;[copy[index - 1], copy[index]] = [copy[index], copy[index - 1]]
                                  setAttributes(copy)
                                }}
                              >
                                ↑
                              </button>
                              <button
                                type="button"
                                className="btn btn-outline-secondary btn-sm"
                                disabled={index === attributes.length - 1}
                                onClick={() => {
                                  const copy = [...attributes]
                                  ;[copy[index], copy[index + 1]] = [copy[index + 1], copy[index]]
                                  setAttributes(copy)
                                }}
                              >
                                ↓
                              </button>
                            </div>
                          </td>
                        </tr>
                      ))}
                    </tbody>
                  </table>
                </div>
              )}
            </div>
          )}

          {/* STEP 3: ACCESS RULES */}
          {step === 'Access Rules' && (
            <div className="form-step-content">
              {isPublic ? (
                <div className="alert alert-info">
                  {t('This position is configured as Public. Candidates do not need to satisfy special access filters to submit their CV. To configure access rules, switch visibility to Restricted in Step 1.')}
                </div>
              ) : (
                <>
                  <p className="text-muted">
                    {t('Candidates must meet all defined criteria in their profile to gain access to create a CV for this position.')}
                  </p>

                  {rules.map((rule, index) => {
                    const definition = ruleDefinitions[rule.attributeId]
                    const type = definition?.type
                    const operators =
                      type === 'Numeric'
                        ? ['Equals', 'GreaterThan', 'GreaterThanOrEqual', 'LessThan', 'LessThanOrEqual']
                        : type === 'String' || type === 'Text'
                        ? ['Equals', 'Contains']
                        : ['Equals']

                    const update = (change: Partial<Rule>) =>
                      setRules(rules.map((r, pos) => (pos === index ? { ...r, ...change } : r)))

                    return (
                      <div className="rule-builder-row" key={index}>
                        <select
                          className="form-select form-select-sm"
                          value={rule.attributeId}
                          onChange={e => update({ attributeId: e.target.value, operator: 'Equals', comparisonValue: '' })}
                        >
                          <option value="">{t('Select Attribute...')}</option>
                          {library.data?.items
                            .filter(item => !item.isBuiltIn)
                            .map(item => (
                              <option value={item.id} key={item.id}>
                                {item.name}
                              </option>
                            ))}
                        </select>

                        <select
                          className="form-select form-select-sm"
                          value={rule.operator}
                          onChange={e => update({ operator: e.target.value })}
                        >
                          {operators.map(item => (
                            <option key={item}>{item}</option>
                          ))}
                        </select>

                        {type === 'Dropdown' ? (
                          <select
                            className="form-select form-select-sm"
                            value={rule.comparisonValue}
                            onChange={e => update({ comparisonValue: e.target.value })}
                          >
                            <option value="">{t('Select option')}</option>
                            {definition.options.map(option => (
                              <option key={option.id} value={option.id}>
                                {option.value}
                              </option>
                            ))}
                          </select>
                        ) : type === 'Boolean' ? (
                          <select
                            className="form-select form-select-sm"
                            value={rule.comparisonValue}
                            onChange={e => update({ comparisonValue: e.target.value })}
                          >
                            <option value="">{t('Select value')}</option>
                            <option value="true">{t('Yes')}</option>
                            <option value="false">{t('No')}</option>
                          </select>
                        ) : (
                          <input
                            className="form-control form-control-sm"
                            type={type === 'Numeric' ? 'number' : 'text'}
                            step={type === 'Numeric' ? 'any' : undefined}
                            value={rule.comparisonValue}
                            onChange={e => update({ comparisonValue: e.target.value })}
                            placeholder={t('Value')}
                          />
                        )}

                        <button
                          type="button"
                          className="btn btn-outline-danger btn-sm"
                          onClick={() => setRules(rules.filter((_, pos) => pos !== index))}
                        >
                          ✕
                        </button>
                      </div>
                    )
                  })}

                  <button
                    type="button"
                    className="btn btn-outline-primary btn-sm mt-2"
                    onClick={() => setRules([...rules, { attributeId: '', operator: 'Equals', comparisonValue: '' }])}
                  >
                    {t('+ Add access rule')}
                  </button>
                </>
              )}
            </div>
          )}

          {/* STEP 4: PROJECTS */}
          {step === 'Projects' && (
            <div className="form-step-content">
              <div className="form-field-group">
                <label>{t('Technology tags (comma separated)')}</label>
                <input
                  className="form-control"
                  value={tags}
                  onChange={e => setTags(e.target.value)}
                  placeholder="e.g. .NET, PostgreSQL, Docker"
                />
                <span className="helper-text">
                  {t('CV generator will automatically include candidate projects tagged with these technologies.')}
                </span>
              </div>

              <div className="form-field-group">
                <label>{t('Maximum projects')}</label>
                <input
                  className="form-control"
                  type="number"
                  min={0}
                  max={20}
                  value={maxProjects}
                  onChange={e => setMaxProjects(Number(e.target.value))}
                />
                <span className="helper-text">{t('Maximum number of relevant projects included in the generated CV.')}</span>
              </div>
            </div>
          )}

          {/* STEP 5: PREVIEW */}
          {step === 'Preview' && (
            <div className="form-step-content">
              <div className="pos-card">
                <h2 style={{ fontSize: '1.4rem', fontWeight: 700, margin: '0 0 0.5rem 0' }}>
                  {title || t('Untitled Position')}
                </h2>
                <p className="text-muted mb-3">
                  {company || t('No company')} · {level || t('Any level')} · {isPublic ? t('Public') : t('Restricted')}
                </p>
                <p style={{ lineHeight: 1.6 }}>{description || t('No description provided.')}</p>

                <h4 style={{ fontSize: '1rem', fontWeight: 600, marginTop: '1.25rem' }}>{t('Attributes')} ({attributes.length})</h4>
                <div className="pos-attr-chips mb-3">
                  {attributes.map(a => (
                    <span className={`pos-attr-chip ${a.isRequired ? 'required' : ''}`} key={a.attributeId}>
                      {a.name} {a.isRequired && '*'}
                    </span>
                  ))}
                  {attributes.length === 0 && <span className="text-muted">{t('None')}</span>}
                </div>

                <h4 style={{ fontSize: '1rem', fontWeight: 600 }}>{t('Project Constraints')}</h4>
                <p className="text-muted mb-0">
                  {t('Up to')} {maxProjects} {t('projects')} {tags ? `${t('matching:')} ${tags}` : t('(any tags)')}.
                </p>
              </div>
            </div>
          )}

          {/* Footer Actions */}
          <footer className="pos-form-actions">
            <div>
              <a href={id ? `/positions/${id}` : '/positions'} className="btn btn-outline-secondary">
                {t('Cancel')}
              </a>
            </div>

            <div className="stepper-nav-buttons">
              {step !== 'General' && (
                <button type="button" className="btn btn-outline-secondary" onClick={prevStep}>
                  ← {t('Back')}
                </button>
              )}

              {step !== 'Preview' ? (
                <button type="button" className="btn btn-primary" onClick={nextStep}>
                  {t('Next')} →
                </button>
              ) : (
                <button type="submit" className="btn btn-success" disabled={saving}>
                  {saving ? t('Saving...') : t('Save')}
                </button>
              )}
            </div>
          </footer>
        </form>
      </div>

      {/* In-Wizard Attribute Creation Modal */}
      {showNewAttrModal && (
        <div
          className="modal fade show d-block"
          tabIndex={-1}
          role="dialog"
          aria-modal="true"
          style={{ backgroundColor: 'rgba(0, 0, 0, 0.5)', zIndex: 1050 }}
          onClick={() => setShowNewAttrModal(false)}
        >
          <div
            className="modal-dialog modal-dialog-centered"
            role="document"
            style={{ maxWidth: 540 }}
            onClick={e => e.stopPropagation()}
          >
            <form className="modal-content shadow-lg border-0" onSubmit={handleCreateNewAttribute}>
              <div className="modal-header border-bottom">
                <h5 className="modal-title h5 mb-0" style={{ fontWeight: 600 }}>
                  {t('Create attribute')}
                </h5>
                <button
                  type="button"
                  className="btn-close"
                  onClick={() => setShowNewAttrModal(false)}
                  aria-label="Close"
                ></button>
              </div>

              <div className="modal-body py-3">
                {newAttrError && (
                  <div className="alert alert-danger py-2 mb-3" role="alert">
                    {newAttrError}
                  </div>
                )}

                <div className="mb-3">
                  <label className="form-label fw-semibold">{t('Name')} *</label>
                  <input
                    className="form-control"
                    value={newAttrName}
                    onChange={e => setNewAttrName(e.target.value)}
                    required
                    maxLength={160}
                    placeholder={t('e.g. Docker, English Level, CAP')}
                    autoFocus
                  />
                </div>

                <div className="row g-2 mb-3">
                  <div className="col-sm-6">
                    <label className="form-label fw-semibold">{t('Category')}</label>
                    <select
                      className="form-select"
                      value={newAttrCategory}
                      onChange={e => setNewAttrCategory(e.target.value)}
                    >
                      {availableCategories.map(cat => (
                        <option key={cat} value={cat}>
                          {cat}
                        </option>
                      ))}
                    </select>
                  </div>

                  <div className="col-sm-6">
                    <label className="form-label fw-semibold">{t('Type')}</label>
                    <select
                      className="form-select"
                      value={newAttrType}
                      onChange={e => setNewAttrType(e.target.value as AttributeType)}
                    >
                      {['String', 'Text', 'Numeric', 'Date', 'Period', 'Boolean', 'Dropdown', 'Image'].map(tp => (
                        <option key={tp} value={tp}>
                          {tp}
                        </option>
                      ))}
                    </select>
                  </div>
                </div>

                {newAttrType === 'Dropdown' && (
                  <div className="border rounded p-3 bg-light-subtle mb-3">
                    <h6 className="fw-bold mb-2">{t('Dropdown Options')}</h6>
                    <div className="d-grid gap-2 mb-2">
                      {newAttrOptions.map((opt, idx) => (
                        <div className="d-flex gap-2" key={idx}>
                          <input
                            className="form-control form-control-sm"
                            value={opt}
                            onChange={e =>
                              setNewAttrOptions(
                                newAttrOptions.map((o, p) => (p === idx ? e.target.value : o))
                              )
                            }
                            placeholder={`${t('Option')} ${idx + 1}`}
                            required
                          />
                          <button
                            type="button"
                            className="btn btn-outline-danger btn-sm"
                            disabled={newAttrOptions.length <= 1}
                            onClick={() => setNewAttrOptions(newAttrOptions.filter((_, p) => p !== idx))}
                          >
                            ×
                          </button>
                        </div>
                      ))}
                    </div>
                    <button
                      type="button"
                      className="btn btn-outline-primary btn-sm"
                      onClick={() => setNewAttrOptions([...newAttrOptions, ''])}
                    >
                      + {t('Add option')}
                    </button>
                  </div>
                )}

                <div className="mb-2">
                  <label className="form-label fw-semibold">{t('Description')}</label>
                  <textarea
                    className="form-control"
                    rows={2}
                    value={newAttrDesc}
                    onChange={e => setNewAttrDesc(e.target.value)}
                    placeholder={t('Provide guidance on how to evaluate or fill this attribute...')}
                    maxLength={2000}
                  />
                </div>
              </div>

              <div className="modal-footer border-top">
                <button
                  type="button"
                  className="btn btn-outline-secondary btn-sm"
                  onClick={() => setShowNewAttrModal(false)}
                >
                  {t('Cancel')}
                </button>
                <button
                  type="submit"
                  className="btn btn-primary btn-sm"
                  disabled={creatingAttr}
                >
                  {creatingAttr ? t('Creating...') : t('Create attribute')}
                </button>
              </div>
            </form>
          </div>
        </div>
      )}
    </div>
  )
}
