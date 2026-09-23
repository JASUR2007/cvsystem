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

    if (mode === 'register' && data.password !== data.confirmPassword) {
      setError(t('Passwords do not match.'))
      setSubmitting(false)
      return
    }

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
        {mode === 'register' && <label>{t('Confirm password')}<input name="confirmPassword" type="password" autoComplete="new-password" minLength={8} required /></label>}
        {error && <div className="auth-error" role="alert">{error}</div>}
        <button className="auth-submit" disabled={submitting}>{t(submitting ? 'Please wait...' : mode === 'login' ? 'Sign in' : 'Create account')}</button>
      </form>
      <div className="social-auth">
        <div className="auth-divider"><span>{t('or continue with')}</span></div>
        {providers?.google ? <a className="social-button" href={`${apiBase}/api/auth/external/google`}><GoogleIcon />{t('Continue with Google')}</a> : <button className="social-button" type="button" disabled><GoogleIcon />{t('Continue with Google')}</button>}
        {providers?.github ? <a className="social-button" href={`${apiBase}/api/auth/external/github`}><GitHubIcon />{t('Continue with GitHub')}</a> : <button className="social-button" type="button" disabled><GitHubIcon />{t('Continue with GitHub')}</button>}
      </div>
      <div className="auth-switch">{t(mode === 'login' ? "Don't have an account?" : 'Already have an account?')} <a href={mode === 'login' ? '/register' : '/login'}>{t(mode === 'login' ? 'Register' : 'Sign in')}</a></div>
    </div>
  </section>
}

function GoogleIcon() {
  return <svg viewBox="0 0 24 24" aria-hidden="true"><path fill="#4285F4" d="M21.6 12.2c0-.7-.1-1.5-.2-2.2H12v4.3h5.4a4.6 4.6 0 0 1-2 3v2.8h3.5c2-1.9 3.2-4.6 3.2-7.9Z"/><path fill="#34A853" d="M12 22c2.9 0 5.3-1 7-2.6l-3.5-2.8c-1 .7-2.1 1-3.5 1a6.1 6.1 0 0 1-5.7-4.2H2.7v2.9A10.6 10.6 0 0 0 12 22Z"/><path fill="#FBBC05" d="M6.3 13.4A6.4 6.4 0 0 1 6 11.6c0-.6.1-1.2.3-1.8V7H2.7a10.5 10.5 0 0 0 0 9.3l3.6-2.9Z"/><path fill="#EA4335" d="M12 5.6c1.6 0 3 .6 4.1 1.6l3.1-3.1A10.4 10.4 0 0 0 2.7 7l3.6 2.8A6.1 6.1 0 0 1 12 5.6Z"/></svg>
}

function GitHubIcon() {
  return <svg viewBox="0 0 24 24" aria-hidden="true"><path fill="currentColor" d="M12 2a10 10 0 0 0-3.2 19.5c.5.1.7-.2.7-.5v-2c-2.8.6-3.4-1.2-3.4-1.2-.5-1.2-1.1-1.5-1.1-1.5-.9-.6.1-.6.1-.6 1 0 1.5 1 1.5 1 .9 1.5 2.3 1.1 2.9.9.1-.6.3-1.1.6-1.3-2.3-.3-4.7-1.1-4.7-5A4 4 0 0 1 6.5 8.5c-.1-.3-.5-1.3.1-2.7 0 0 .9-.3 2.8 1.1a9.6 9.6 0 0 1 5.2 0c2-1.4 2.8-1.1 2.8-1.1.6 1.4.2 2.4.1 2.7a4 4 0 0 1 1.1 2.8c0 3.9-2.4 4.7-4.7 5 .4.3.7 1 .7 2V21c0 .3.2.6.7.5A10 10 0 0 0 12 2Z"/></svg>
}
