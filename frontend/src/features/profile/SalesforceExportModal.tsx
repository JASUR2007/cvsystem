import { useState, useId } from 'react'
import { api, json, type Profile, type SalesforceExportRequest, type SalesforceExportResult } from '../../shared/api'
import { t } from '../../shared/i18n'

type Props = {
  isOpen: boolean
  onClose: () => void
  profile: Profile
  userId?: string
}

const INDUSTRIES = [
  'Technology',
  'Finance & Banking',
  'Consulting',
  'Healthcare',
  'Education',
  'Retail & E-commerce',
  'Manufacturing',
  'Other'
]

export function SalesforceExportModal({ isOpen, onClose, profile, userId }: Props) {
  const [accountName, setAccountName] = useState('')
  const [phone, setPhone] = useState('')
  const [title, setTitle] = useState('')
  const [industry, setIndustry] = useState('Technology')
  const [description, setDescription] = useState('')

  const [loading, setLoading] = useState(false)
  const [error, setError] = useState<string | null>(null)
  const [result, setResult] = useState<SalesforceExportResult | null>(null)

  const accountIdInput = useId()
  const phoneInput = useId()
  const titleInput = useId()
  const industryInput = useId()
  const descInput = useId()

  if (!isOpen) return null

  async function handleSubmit(e: React.FormEvent) {
    e.preventDefault()
    setLoading(true)
    setError(null)
    setResult(null)

    try {
      const payload: SalesforceExportRequest = {
        accountName: accountName.trim() || undefined,
        phone: phone.trim() || undefined,
        title: title.trim() || undefined,
        industry: industry || undefined,
        description: description.trim() || undefined
      }

      const query = userId ? `?userId=${userId}` : ''
      const res = await api<SalesforceExportResult>(`/profile/salesforce${query}`, json('POST', payload))
      setResult(res)
    } catch (err) {
      setError(err instanceof Error ? err.message : 'Failed to export to Salesforce.')
    } finally {
      setLoading(false)
    }
  }

  function handleClose() {
    setError(null)
    setResult(null)
    onClose()
  }

  return (
    <div
      className="modal show d-block"
      tabIndex={-1}
      role="dialog"
      aria-modal="true"
      style={{ backgroundColor: 'rgba(0,0,0,0.5)', zIndex: 1050 }}
    >
      <div className="modal-dialog modal-dialog-centered modal-lg" role="document">
        <div className="modal-content border-0 shadow">
          {/* Header */}
          <div className="modal-header border-bottom pb-3" style={{ background: 'linear-gradient(135deg, #00A1E0 0%, #0070D2 100%)', color: '#ffffff' }}>
            <div className="d-flex align-items-center gap-2 text-white">
              <svg width="24" height="24" viewBox="0 0 24 24" fill="none" stroke="#ffffff" strokeWidth="2" strokeLinecap="round" strokeLinejoin="round" style={{ color: '#ffffff' }}>
                <path d="M18 10h-1.26A8 8 0 1 0 9 20h9a5 5 0 0 0 0-10z" />
              </svg>
              <h5 className="modal-title fw-bold mb-0 text-white" style={{ color: '#ffffff' }}>{t('Salesforce CRM Integration')}</h5>
            </div>
            <button
              type="button"
              className="btn-close btn-close-white"
              aria-label="Close"
              onClick={handleClose}
              disabled={loading}
            />
          </div>

          <form onSubmit={handleSubmit}>
            <div className="modal-body p-4">
              <p className="text-muted small mb-3">
                {t('Manage user relationships, sync profile contacts, and create Accounts in Salesforce.')}
              </p>

              {/* Read-only profile metadata badge box */}
              <div className="p-3 mb-3 rounded bg-light border">
                <div className="fw-semibold text-secondary small mb-2">
                  {t('The following details will be automatically sent from your profile:')}
                </div>
                <div className="row g-2 small">
                  <div className="col-sm-6">
                    <strong>{t('First name')}:</strong> <span className="text-dark">{profile.firstName || '-'}</span>
                  </div>
                  <div className="col-sm-6">
                    <strong>{t('Last name')}:</strong> <span className="text-dark">{profile.lastName || '-'}</span>
                  </div>
                  <div className="col-sm-6">
                    <strong>{t('Email')}:</strong> <span className="text-dark">{profile.email}</span>
                  </div>
                  <div className="col-sm-6">
                    <strong>{t('Location')}:</strong> <span className="text-dark">{profile.location || t('Location not set')}</span>
                  </div>
                </div>
              </div>

              {/* Status / Alert feedback */}
              {error && (
                <div className="alert alert-danger d-flex align-items-center gap-2 py-2" role="alert">
                  <svg width="18" height="18" viewBox="0 0 24 24" fill="none" stroke="currentColor" strokeWidth="2">
                    <circle cx="12" cy="12" r="10" />
                    <line x1="12" y1="8" x2="12" y2="12" />
                    <line x1="12" y1="16" x2="12.01" y2="16" />
                  </svg>
                  <div>{error}</div>
                </div>
              )}

              {result && (
                <div className="alert alert-success py-3 mb-3" role="alert">
                  <div className="fw-bold mb-1">
                    ✓ {t('Successfully created Account and Contact in Salesforce CRM!')}
                  </div>
                  <div className="small text-muted mb-2">
                    {t('Successfully exported user to Salesforce CRM.')}
                  </div>
                  <div className="d-flex flex-wrap gap-3 small">
                    <div>
                      <strong>{t('Account ID')}:</strong> <code className="bg-white px-2 py-1 rounded border">{result.accountId}</code>
                    </div>
                    <div>
                      <strong>{t('Contact ID')}:</strong> <code className="bg-white px-2 py-1 rounded border">{result.contactId}</code>
                    </div>
                  </div>
                  {result.instanceUrl && (
                    <div className="mt-2 pt-2 border-top">
                      <a
                        href={`${result.instanceUrl}/${result.contactId}`}
                        target="_blank"
                        rel="noreferrer"
                        className="btn btn-outline-success btn-sm d-inline-flex align-items-center gap-1"
                      >
                        <svg width="14" height="14" viewBox="0 0 24 24" fill="none" stroke="currentColor" strokeWidth="2">
                          <path d="M18 13v6a2 2 0 0 1-2 2H5a2 2 0 0 1-2-2V8a2 2 0 0 1 2-2h6" />
                          <polyline points="15 3 21 3 21 9" />
                          <line x1="10" y1="14" x2="21" y2="3" />
                        </svg>
                        <span>{t('Open in Salesforce')}</span>
                      </a>
                    </div>
                  )}
                </div>
              )}

              {/* Form Fields */}
              <div className="row g-3">
                <div className="col-md-6">
                  <label htmlFor={accountIdInput} className="form-label small fw-semibold">
                    {t('Company / Account Name')}
                  </label>
                  <input
                    id={accountIdInput}
                    type="text"
                    className="form-control form-control-sm"
                    placeholder={`${profile.firstName} ${profile.lastName} (TalentHub)`}
                    value={accountName}
                    onChange={e => setAccountName(e.target.value)}
                    disabled={loading}
                  />
                  <small className="text-muted" style={{ fontSize: '0.75rem' }}>
                    {t('Leave empty to default to user name.')}
                  </small>
                </div>

                <div className="col-md-6">
                  <label htmlFor={phoneInput} className="form-label small fw-semibold">
                    {t('Phone')}
                  </label>
                  <input
                    id={phoneInput}
                    type="tel"
                    className="form-control form-control-sm"
                    placeholder="+1 (555) 000-0000"
                    value={phone}
                    onChange={e => setPhone(e.target.value)}
                    disabled={loading}
                  />
                </div>

                <div className="col-md-6">
                  <label htmlFor={titleInput} className="form-label small fw-semibold">
                    {t('Job Title')}
                  </label>
                  <input
                    id={titleInput}
                    type="text"
                    className="form-control form-control-sm"
                    placeholder="e.g. Senior Software Engineer / Recruiter"
                    value={title}
                    onChange={e => setTitle(e.target.value)}
                    disabled={loading}
                  />
                </div>

                <div className="col-md-6">
                  <label htmlFor={industryInput} className="form-label small fw-semibold">
                    {t('Industry')}
                  </label>
                  <select
                    id={industryInput}
                    className="form-select form-select-sm"
                    value={industry}
                    onChange={e => setIndustry(e.target.value)}
                    disabled={loading}
                  >
                    {INDUSTRIES.map(item => (
                      <option key={item} value={item}>
                        {item}
                      </option>
                    ))}
                  </select>
                </div>

                <div className="col-12">
                  <label htmlFor={descInput} className="form-label small fw-semibold">
                    {t('Notes / Description')}
                  </label>
                  <textarea
                    id={descInput}
                    rows={3}
                    className="form-control form-control-sm"
                    placeholder="Candidate skills, recruitment notes, CRM remarks..."
                    value={description}
                    onChange={e => setDescription(e.target.value)}
                    disabled={loading}
                  />
                </div>
              </div>
            </div>

            {/* Modal Footer */}
            <div className="modal-footer border-top bg-light">
              <button
                type="button"
                className="btn btn-secondary btn-sm"
                onClick={handleClose}
                disabled={loading}
              >
                {t('Close')}
              </button>
              <button
                type="submit"
                className="btn btn-primary btn-sm d-inline-flex align-items-center gap-2"
                disabled={loading}
                style={{ backgroundColor: '#0070D2', borderColor: '#0070D2' }}
              >
                {loading ? (
                  <>
                    <span className="spinner-border spinner-border-sm" role="status" aria-hidden="true" />
                    <span>{t('Synchronizing with Salesforce...')}</span>
                  </>
                ) : (
                  <>
                    <svg width="16" height="16" viewBox="0 0 24 24" fill="none" stroke="currentColor" strokeWidth="2">
                      <path d="M18 10h-1.26A8 8 0 1 0 9 20h9a5 5 0 0 0 0-10z" />
                    </svg>
                    <span>{t('Export to Salesforce')}</span>
                  </>
                )}
              </button>
            </div>
          </form>
        </div>
      </div>
    </div>
  )
}
