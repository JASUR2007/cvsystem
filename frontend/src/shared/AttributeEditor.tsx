import type { AttributeValue } from './api'
import ImageUploader from './ImageUploader'
import { t } from './i18n'

export default function AttributeEditor({ value, options = [], onChange, userId }: { value: AttributeValue; options?: { id: string; value: string }[]; onChange: (value: AttributeValue) => void; userId?: string }) {
  const change = (field: keyof AttributeValue, next: string | number | boolean | null) => onChange({ ...value, [field]: next })
  switch (value.type) {
    case 'String': return <input className="form-control" value={value.textValue ?? ''} onChange={event => change('textValue', event.target.value)} />
    case 'Text': return <textarea className="form-control" rows={3} value={value.textValue ?? ''} onChange={event => change('textValue', event.target.value)} />
    case 'Numeric': return <input className="form-control" type="number" step="any" value={value.numberValue ?? ''} onChange={event => change('numberValue', event.target.value === '' ? null : Number(event.target.value))} />
    case 'Date': return <input className="form-control" type="date" value={value.dateValue ?? ''} onChange={event => change('dateValue', event.target.value || null)} />
    case 'Period': return <div className="period-fields"><input className="form-control" type="date" aria-label="Start date" value={value.periodStart ?? ''} onChange={event => change('periodStart', event.target.value || null)} /><input className="form-control" type="date" aria-label="End date" value={value.periodEnd ?? ''} onChange={event => change('periodEnd', event.target.value || null)} /></div>
    case 'Boolean': return <select className="form-select" value={value.booleanValue === null ? '' : String(value.booleanValue)} onChange={event => change('booleanValue', event.target.value === '' ? null : event.target.value === 'true')}><option value="">{t('Not set')}</option><option value="true">{t('Yes')}</option><option value="false">{t('No')}</option></select>
    case 'Dropdown': return <select className="form-select" value={value.selectedOptionId ?? ''} onChange={event => change('selectedOptionId', event.target.value || null)}><option value="">{t('Select...')}</option>{options.map(option => <option key={option.id} value={option.id}>{option.value}</option>)}</select>
    case 'Image': return <ImageUploader value={value.imageObjectKey} onChange={key => change('imageObjectKey', key)} userId={userId} />
  }
}
