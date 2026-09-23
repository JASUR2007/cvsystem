import { useEffect, useState, type FormEvent } from 'react'
import { apiBase, saveToken, submitAuth, type CurrentUser } from './api'
import { t } from '../shared/i18n'

type Props = {
  mode: 'login' | 'register'
  onAuthenticated: (user: CurrentUser) => void
}

export default function AuthPage({ mode, onAuthenticated }: Props) {
  const [error, setError] = useState('')
  const [submitting, setSubmitting] = useState(false)
  const [providers, setProviders] = useState<{ google: boolean; github: boolean } | null>(null)

  useEffect(() => { fetch(`${apiBase}/api/auth/providers`).then(response => response.json()).then(setProviders).catch(() => undefined) }, [])

  async function handleSubmit(event: FormEvent<HTMLFormElement>) {
    event.preventDefault()
    setError('')
    setSubmitting(true)

    const form = new FormData(event.currentTarget)
    const data = Object.fromEntries(form.entries()) as Record<string, string>

    try {
      const result = await submitAuth(mode, data)
      saveToken(result.token)
      onAuthenticated(result.user)
      window.location.assign('/')
    } catch (cause) {
      setError(cause instanceof Error ? cause.message : 'Something went wrong. Please try again.')
    } finally {
      setSubmitting(false)
    }
  }

  return <section className="auth-shell">
    <div className="auth-panel">
      <span className="eyebrow">TALENTHUB ACCOUNT</span>
      <h1>{t(mode === 'login' ? 'Welcome back' : 'Create your account')}</h1>
      <p>{t(mode === 'login' ? 'Sign in to continue your work.' : 'Join TalentHub and start your journey.')}</p>
      <form onSubmit={handleSubmit}>
        {mode === 'register' && <div className="name-fields"><label>{t('First name')}<input name="firstName" autoComplete="given-name" maxLength={100} required /></label><label>{t('Last name')}<input name="lastName" autoComplete="family-name" maxLength={100} required /></label></div>}
        <label>{t('Email')}<input name="email" type="email" autoComplete="email" required /></label>
        <label>{t('Password')}<input name="password" type="password" autoComplete={mode === 'login' ? 'current-password' : 'new-password'} minLength={mode === 'register' ? 8 : undefined} required /></label>
        {error && <div className="auth-error" role="alert">{error}</div>}
        <button className="auth-submit" disabled={submitting}>{t(submitting ? 'Please wait...' : mode === 'login' ? 'Sign in' : 'Create account')}</button>
      </form>
      {(providers?.google || providers?.github) && <div className="social-auth"><span>{t('or continue with')}</span>{providers.google && <a className="btn btn-outline-secondary" href={`${apiBase}/api/auth/external/google`}>{t('Continue with Google')}</a>}{providers.github && <a className="btn btn-outline-secondary" href={`${apiBase}/api/auth/external/github`}>{t('Continue with GitHub')}</a>}</div>}
      <div className="auth-switch">{t(mode === 'login' ? "Don't have an account?" : 'Already have an account?')} <a href={mode === 'login' ? '/register' : '/login'}>{t(mode === 'login' ? 'Register' : 'Sign in')}</a></div>
    </div>
  </section>
}
