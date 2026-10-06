import { useState } from 'react'
import { api, type PositionTokenResponse, type PositionTokenStatusResponse, dateText, getBackendApiBaseUrl } from '../../shared/api'
import { useApi } from '../../shared/useApi'
import { t } from '../../shared/i18n'

type Props = {
  positionId: string
}

export function PositionOdooIntegration({ positionId }: Props) {
  const statusApi = useApi<PositionTokenStatusResponse>(`/positions/${positionId}/odoo-token/status`)
  const [generatedToken, setGeneratedToken] = useState<string | null>(null)
  const [loading, setLoading] = useState(false)
  const [error, setError] = useState<string | null>(null)
  const [successMsg, setSuccessMsg] = useState<string | null>(null)
  const [tokenCopied, setTokenCopied] = useState(false)
  const [endpointCopied, setEndpointCopied] = useState(false)
  const [sourceCopied, setSourceCopied] = useState(false)

  const backendBase = getBackendApiBaseUrl()
  const endpointUrl = `${backendBase}/api/integrations/odoo/position-results`
  const sourceUrl = `${window.location.origin}/positions/${positionId}`

  async function handleGenerateOrRegenerate() {
    setLoading(true)
    setError(null)
    setSuccessMsg(null)

    try {
      const res = await api<PositionTokenResponse>(`/positions/${positionId}/odoo-token`, { method: 'POST' })
      setGeneratedToken(res.token)
      setSuccessMsg(t('Token generated successfully.'))
      statusApi.reload()
    } catch (err) {
      setError(err instanceof Error ? err.message : t('Could not generate token.'))
    } finally {
      setLoading(false)
    }
  }

  async function copyToken() {
    if (!generatedToken) return
    try {
      await navigator.clipboard.writeText(generatedToken)
      setTokenCopied(true)
      setTimeout(() => setTokenCopied(false), 2500)
    } catch {
      // ignore clipboard error
    }
  }

  async function copyEndpoint() {
    try {
      await navigator.clipboard.writeText(endpointUrl)
      setEndpointCopied(true)
      setTimeout(() => setEndpointCopied(false), 2500)
    } catch {
      // ignore clipboard error
    }
  }

  async function copySource() {
    try {
      await navigator.clipboard.writeText(sourceUrl)
      setSourceCopied(true)
      setTimeout(() => setSourceCopied(false), 2500)
    } catch {
      // ignore clipboard error
    }
  }

  const hasToken = statusApi.data?.hasToken ?? false

  return (
    <div className="card shadow-sm border-0 mt-4 overflow-hidden" style={{ borderRadius: '0.75rem' }}>
      <div
        className="card-header py-3 px-4 d-flex align-items-center justify-content-between"
        style={{
          background: 'linear-gradient(135deg, #714B67 0%, #56334D 100%)',
          color: '#ffffff',
        }}
      >
        <div className="d-flex align-items-center gap-2">
          <svg width="22" height="22" viewBox="0 0 24 24" fill="none" stroke="currentColor" strokeWidth="2" strokeLinecap="round" strokeLinejoin="round">
            <rect x="2" y="3" width="20" height="14" rx="2" ry="2" />
            <line x1="8" y1="21" x2="16" y2="21" />
            <line x1="12" y1="17" x2="12" y2="21" />
          </svg>
          <h5 className="mb-0 fw-bold" style={{ fontSize: '1.05rem', color: '#fff' }}>
            {t('Odoo Integration')}
          </h5>
        </div>
        <span className="badge bg-white text-dark small" style={{ fontWeight: 600 }}>
          API Token
        </span>
      </div>

      <div className="card-body p-4 bg-white">
        <p className="text-muted mb-3" style={{ fontSize: '0.9rem' }}>
          {t('Export aggregated Position results to an external Odoo instance using a Position-specific API token.')}
        </p>

        {error && (
          <div className="alert alert-danger py-2 px-3 small d-flex align-items-center gap-2" role="alert">
            <svg width="16" height="16" viewBox="0 0 24 24" fill="none" stroke="currentColor" strokeWidth="2">
              <circle cx="12" cy="12" r="10" />
              <line x1="12" y1="8" x2="12" y2="12" />
              <line x1="12" y1="16" x2="12.01" y2="16" />
            </svg>
            <span>{error}</span>
          </div>
        )}

        {successMsg && (
          <div className="alert alert-success py-2 px-3 small d-flex align-items-center gap-2" role="alert">
            <svg width="16" height="16" viewBox="0 0 24 24" fill="none" stroke="currentColor" strokeWidth="2">
              <polyline points="20 6 9 17 4 12" />
            </svg>
            <span>{successMsg}</span>
          </div>
        )}

        {/* Existing Token Status (when not newly generated in current session) */}
        {!generatedToken && hasToken && (
          <div className="p-3 mb-3 rounded bg-light border">
            <div className="d-flex align-items-center gap-2 text-secondary mb-2 small fw-semibold">
              <svg width="16" height="16" viewBox="0 0 24 24" fill="none" stroke="currentColor" strokeWidth="2">
                <circle cx="12" cy="12" r="10" />
                <polyline points="12 6 12 12 16 14" />
              </svg>
              <span>{t('An API token already exists. Regenerate it if you need a new token.')}</span>
            </div>
            <div className="row g-2 small text-muted">
              <div className="col-sm-6">
                <strong>{t('Created')}:</strong> {dateText(statusApi.data?.createdAt)}
              </div>
              <div className="col-sm-6">
                <strong>{t('Last used')}:</strong>{' '}
                {statusApi.data?.lastUsedAt ? dateText(statusApi.data.lastUsedAt) : t('Never used')}
              </div>
            </div>
          </div>
        )}

        {/* Newly Generated Token View */}
        {generatedToken && (
          <div className="mb-4">
            <label className="form-label fw-semibold small text-dark mb-1">
              {t('API Token')}
            </label>
            <div className="input-group mb-2">
              <input
                type="text"
                readOnly
                className="form-control font-monospace"
                style={{ fontSize: '0.85rem', backgroundColor: '#F8FAFC' }}
                value={generatedToken}
              />
              <button
                type="button"
                className={`btn ${tokenCopied ? 'btn-success' : 'btn-outline-secondary'}`}
                onClick={copyToken}
                title={t('Copy')}
              >
                {tokenCopied ? t('Copied!') : t('Copy')}
              </button>
            </div>

            <div className="alert alert-warning py-2 px-3 small d-flex align-items-center gap-2 mb-3" role="alert">
              <svg width="18" height="18" viewBox="0 0 24 24" fill="none" stroke="currentColor" strokeWidth="2" className="flex-shrink-0">
                <path d="M10.29 3.86L1.82 18a2 2 0 0 0 1.71 3h16.94a2 2 0 0 0 1.71-3L13.71 3.86a2 2 0 0 0-3.42 0z" />
                <line x1="12" y1="9" x2="12" y2="13" />
                <line x1="12" y1="17" x2="12.01" y2="17" />
              </svg>
              <span>{t('This token grants read-only access to aggregated results for this Position. Keep it private.')}</span>
            </div>

            {/* Public Endpoint & Source URL Information */}
            <div className="p-3 rounded border bg-light">
              <div className="fw-semibold small text-secondary mb-2">
                {t('External API Endpoint (Backend)')}
              </div>
              <div className="mb-2">
                <div className="input-group input-group-sm">
                  <input
                    type="text"
                    readOnly
                    className="form-control font-monospace small"
                    value={endpointUrl}
                  />
                  <button
                    type="button"
                    className={`btn btn-sm ${endpointCopied ? 'btn-success' : 'btn-outline-secondary'}`}
                    onClick={copyEndpoint}
                  >
                    {endpointCopied ? t('Copied!') : t('Copy Endpoint')}
                  </button>
                </div>
                <div className="text-muted mt-1" style={{ fontSize: '0.75rem' }}>
                  {t('Use this endpoint in Odoo to fetch aggregated statistics.')}
                </div>
              </div>
              <div className="small text-muted mb-3">
                <strong>Header:</strong> <code className="bg-white px-2 py-1 rounded border">X-API-Token: {generatedToken}</code>
              </div>

              <div className="pt-2 border-top">
                <div className="fw-semibold small text-secondary mb-1">
                  {t('Source URL (TalentHub Position Web Page)')}
                </div>
                <div className="input-group input-group-sm">
                  <input
                    type="text"
                    readOnly
                    className="form-control font-monospace small"
                    value={sourceUrl}
                  />
                  <button
                    type="button"
                    className={`btn btn-sm ${sourceCopied ? 'btn-success' : 'btn-outline-secondary'}`}
                    onClick={copySource}
                  >
                    {sourceCopied ? t('Copied!') : t('Copy Source URL')}
                  </button>
                </div>
                <div className="text-muted mt-1" style={{ fontSize: '0.75rem' }}>
                  {t('Use this URL in Odoo as Source URL to link back to the position in TalentHub.')}
                </div>
              </div>
            </div>
          </div>
        )}

        {/* Action Buttons */}
        <div className="pt-2">
          <button
            type="button"
            className="btn btn-sm text-white d-inline-flex align-items-center gap-2"
            style={{ backgroundColor: '#714B67', borderColor: '#714B67' }}
            onClick={handleGenerateOrRegenerate}
            disabled={loading}
          >
            {loading ? (
              <>
                <span className="spinner-border spinner-border-sm" role="status" aria-hidden="true" />
                <span>{t('Generating token...')}</span>
              </>
            ) : (
              <>
                <svg width="15" height="15" viewBox="0 0 24 24" fill="none" stroke="currentColor" strokeWidth="2">
                  <path d="M21.5 2v6h-6M21.34 15.57a10 10 0 1 1-.57-8.38l5.67-5.67" />
                </svg>
                <span>{hasToken ? t('Regenerate Token') : t('Generate API Token')}</span>
              </>
            )}
          </button>
        </div>
      </div>
    </div>
  )
}
