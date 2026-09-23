import { useEffect, useState } from 'react'
import { api } from '../shared/api'
import { saveAuth, type CurrentUser } from './api'
import { t } from '../shared/i18n'

type AuthResponse = { token: string; user: CurrentUser }

export default function AuthCallbackPage({ onAuthenticated }: { onAuthenticated: (user: CurrentUser) => void }) {
  const [error, setError] = useState('')
  useEffect(() => {
    const params = new URLSearchParams(window.location.search)
    const code = params.get('code')
    const failure = params.get('error')
    if (!code) {
      queueMicrotask(() => setError(failure === 'account_exists' ? 'This email already has an account. Sign in with your existing method.' : 'External sign-in failed.'))
      return
    }
    api<AuthResponse>('/auth/external/exchange', { method: 'POST', body: JSON.stringify({ code }) })
      .then(result => { saveAuth(result.token, result.user); onAuthenticated(result.user); window.location.replace('/') })
      .catch(cause => setError(cause instanceof Error ? cause.message : 'External sign-in failed.'))
  }, [onAuthenticated])
  return <section className="auth-shell"><div className="auth-panel"><h1>{t(error ? 'Sign-in failed' : 'Completing sign-in...')}</h1>{error && <><p role="alert">{error}</p><a href="/login">{t('Back to sign in')}</a></>}</div></section>
}
