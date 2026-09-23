import { t } from '../shared/i18n'

export default function NotFoundPage() {
  return (
    <div className="not-found-container" style={{
      minHeight: '65vh',
      display: 'flex',
      alignItems: 'center',
      justifyContent: 'center',
      padding: '40px 20px',
      textAlign: 'center',
    }}>
      <div className="not-found-card" style={{
        maxWidth: '520px',
        width: '100%',
        backgroundColor: 'var(--surface-bg)',
        border: '1px solid var(--bs-border-color)',
        borderRadius: '16px',
        padding: '48px 36px',
        boxShadow: 'var(--shadow-md)',
      }}>
        <div style={{
          width: '76px',
          height: '76px',
          borderRadius: '50%',
          backgroundColor: 'rgba(37, 99, 235, 0.1)',
          color: 'var(--color-primary)',
          display: 'inline-flex',
          alignItems: 'center',
          justifyContent: 'center',
          marginBottom: '24px',
        }}>
          <svg width="38" height="38" viewBox="0 0 24 24" fill="none" stroke="currentColor" strokeWidth="2" strokeLinecap="round" strokeLinejoin="round">
            <circle cx="12" cy="12" r="10" />
            <line x1="12" y1="8" x2="12" y2="12" />
            <line x1="12" y1="16" x2="12.01" y2="16" />
          </svg>
        </div>

        <div style={{
          display: 'inline-block',
          padding: '4px 12px',
          borderRadius: '999px',
          backgroundColor: 'var(--primary-light)',
          color: 'var(--color-primary)',
          fontWeight: 700,
          fontSize: '13px',
          marginBottom: '16px',
          letterSpacing: '0.05em',
        }}>
          ERROR 404
        </div>

        <h1 style={{
          fontSize: '28px',
          fontWeight: 800,
          margin: '0 0 12px',
          letterSpacing: '-0.02em',
          color: 'var(--bs-body-color)',
        }}>
          {t('Page not found')}
        </h1>

        <p style={{
          fontSize: '15px',
          color: 'var(--bs-secondary-color)',
          lineHeight: 1.6,
          margin: '0 0 32px',
        }}>
          {t('The page you are looking for might have been removed, had its name changed, or is temporarily unavailable.')}
        </p>

        <div style={{
          display: 'flex',
          gap: '12px',
          justifyContent: 'center',
          flexWrap: 'wrap',
        }}>
          <a href="/" className="btn btn-primary" style={{ padding: '10px 24px', fontWeight: 600 }}>
            {t('Return home')}
          </a>
          <a href="/positions" className="btn btn-outline-secondary" style={{ padding: '10px 20px', fontWeight: 600 }}>
            {t('Positions')}
          </a>
        </div>
      </div>
    </div>
  )
}
