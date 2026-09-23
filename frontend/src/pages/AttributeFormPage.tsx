import { useEffect, useState, type FormEvent } from 'react'
import { api, json, type AttributeDetail, type AttributeType } from '../shared/api'
import { PageTitle, Status } from '../shared/ui'
import { useApi } from '../shared/useApi'
import { t } from '../shared/i18n'

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

export default function AttributeFormPage({ id }: { id?: string }) {
  const existing = useApi<AttributeDetail>(id ? `/attributes/${id}` : null)
  const categoriesApi = useApi<string[]>('/attribute-categories')
  const [name, setName] = useState('')
  const [description, setDescription] = useState('')
  const [category, setCategory] = useState('Technical Skills')
  const [type, setType] = useState<AttributeType>('String')
  const [options, setOptions] = useState<string[]>([''])
  const [message, setMessage] = useState('')
  const [saving, setSaving] = useState(false)

  const availableCategories =
    categoriesApi.data && categoriesApi.data.length > 0
      ? Array.from(new Set([...categoriesApi.data, ...DEFAULT_CATEGORIES]))
      : DEFAULT_CATEGORIES

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
      await api(
        id ? `/attributes/${id}` : '/attributes',
        json(id ? 'PUT' : 'POST', {
          name,
          description,
          category,
          type,
          options: type === 'Dropdown' ? options.map(option => option.trim()).filter(Boolean) : [],
          version: existing.data?.version ?? 1,
        })
      )
      window.location.assign('/attributes')
    } catch (cause) {
      setMessage(cause instanceof Error ? cause.message : t('Could not save attribute.'))
    } finally {
      setSaving(false)
    }
  }

  if (id && (existing.loading || existing.error)) {
    return <Status loading={existing.loading} error={existing.error} />
  }

  return (
    <section className="surface page-surface narrow" style={{ maxWidth: 720, margin: '0 auto' }}>
      <PageTitle title={id ? 'Edit attribute' : 'Create attribute'} />

      {existing.data?.isBuiltIn && (
        <div className="alert alert-info" role="alert">
          {t('Built-in attributes cannot be edited.')}
        </div>
      )}

      <form onSubmit={save} className="stack-form d-grid gap-3">
        <div>
          <label className="form-label fw-semibold">{t('Name')}</label>
          <input
            className="form-control"
            value={name}
            onChange={event => setName(event.target.value)}
            maxLength={160}
            required
            disabled={existing.data?.isBuiltIn}
            placeholder={t('e.g. Docker, English Level, CAP')}
          />
        </div>

        <div>
          <label className="form-label fw-semibold">{t('Description')}</label>
          <textarea
            className="form-control"
            value={description}
            onChange={event => setDescription(event.target.value)}
            rows={3}
            maxLength={2000}
            disabled={existing.data?.isBuiltIn}
            placeholder={t('Provide guidance on how to evaluate or fill this attribute...')}
            style={{ minHeight: 90, lineHeight: 1.5, resize: 'vertical' }}
          />
        </div>

        <div className="row g-3">
          <div className="col-md-6">
            <label className="form-label fw-semibold">{t('Category')}</label>
            <select
              className="form-select"
              value={category}
              onChange={event => setCategory(event.target.value)}
              disabled={existing.data?.isBuiltIn}
            >
              {availableCategories.map(item => (
                <option key={item} value={item}>
                  {item}
                </option>
              ))}
            </select>
          </div>

          <div className="col-md-6">
            <label className="form-label fw-semibold">{t('Type')}</label>
            <select
              className="form-select"
              value={type}
              onChange={event => setType(event.target.value as AttributeType)}
              disabled={existing.data?.isBuiltIn}
            >
              {['String', 'Text', 'Image', 'Numeric', 'Date', 'Period', 'Boolean', 'Dropdown'].map(item => (
                <option key={item} value={item}>
                  {item}
                </option>
              ))}
            </select>
          </div>
        </div>

        {type === 'Dropdown' && (
          <div className="border rounded p-3 bg-light-subtle">
            <h3 className="h6 fw-bold mb-3">{t('Dropdown Options')}</h3>
            <div className="d-grid gap-2 mb-2">
              {options.map((option, index) => (
                <div className="d-flex gap-2" key={index}>
                  <input
                    className="form-control"
                    value={option}
                    onChange={event =>
                      setOptions(options.map((item, position) => (position === index ? event.target.value : item)))
                    }
                    placeholder={`${t('Option')} ${index + 1}`}
                    required
                  />
                  <button
                    type="button"
                    className="btn btn-outline-danger"
                    onClick={() => setOptions(options.filter((_, position) => position !== index))}
                    disabled={options.length <= 1}
                  >
                    ×
                  </button>
                </div>
              ))}
            </div>
            <button
              type="button"
              className="btn btn-outline-primary btn-sm"
              onClick={() => setOptions([...options, ''])}
            >
              + {t('Add option')}
            </button>
          </div>
        )}

        {message && <div className="alert alert-danger" role="alert">{message}</div>}

        <div className="d-flex justify-content-end gap-2 pt-2">
          <a href="/attributes" className="btn btn-outline-secondary">
            {t('Cancel')}
          </a>
          <button className="btn btn-primary" disabled={saving || existing.data?.isBuiltIn}>
            {saving ? t('Saving...') : t(id ? 'Save changes' : 'Save attribute')}
          </button>
        </div>
      </form>
    </section>
  )
}
