import { useEffect, useState } from 'react'
import AuthPage from './auth/AuthPage'
import AuthCallbackPage from './auth/AuthCallbackPage'
import { getCurrentUser, signOut, type CurrentUser } from './auth/api'
import { api, json } from './shared/api'
import HomePage from './pages/HomePage'
import PositionsPage from './pages/PositionsPage'
import PositionDetailPage from './pages/PositionDetailPage'
import PositionFormPage from './pages/PositionFormPage'
import PositionCvsPage from './pages/PositionCvsPage'
import AttributesPage from './pages/AttributesPage'
import AttributeFormPage from './pages/AttributeFormPage'
import ProfilePage from './pages/profile/ProfilePage'
import CvPage from './pages/CvPage'
import AdminPage from './pages/AdminPage'
import SearchPage from './pages/SearchPage'
import Header from './shared/Header'
import './index.css'
import './App.css'
import './media.css'
import { t } from './shared/i18n'

type Settings = { language: string; theme: string; version: number }
const rawPath = window.location.pathname.replace(/\/$/, '') || '/'
const path = rawPath === '/index.html' ? '/' : rawPath

function Page({ user, onAuth }: { user: CurrentUser | null; onAuth: (user: CurrentUser) => void }) {
  if (path === '/') return <HomePage />
  if (path === '/login' || path === '/register') return <AuthPage mode={path === '/login' ? 'login' : 'register'} onAuthenticated={onAuth} />
  if (path === '/auth/callback') return <AuthCallbackPage onAuthenticated={onAuth} />
  if (path === '/positions') return <PositionsPage user={user} />
  if (path === '/positions/new') return <PositionFormPage />
  if (path === '/attributes') return <AttributesPage />
  if (path === '/attributes/new') return <AttributeFormPage />
  if (path === '/profile') return user ? <ProfilePage /> : <SignInPrompt />
  const adminProfile = path.match(/^\/profile\/([0-9a-f-]+)$/i)
  if (adminProfile) return user?.roles.includes('Administrator') ? <ProfilePage userId={adminProfile[1]} /> : <SignInPrompt />
  if (path === '/admin' || path === '/admin/dashboard' || path === '/admin/users') return <AdminPage />
  if (path === '/search') return <SearchPage user={user} />
  const editPosition = path.match(/^\/positions\/([0-9a-f-]+)\/edit$/i)
  if (editPosition) return <PositionFormPage id={editPosition[1]} />
  const positionCvs = path.match(/^\/positions\/([0-9a-f-]+)\/cvs$/i)
  if (positionCvs) return <PositionCvsPage id={positionCvs[1]} />
  const position = path.match(/^\/positions\/([0-9a-f-]+)$/i)
  if (position) return <PositionDetailPage id={position[1]} user={user} />
  const editAttribute = path.match(/^\/attributes\/([0-9a-f-]+)\/edit$/i)
  if (editAttribute) return <AttributeFormPage id={editAttribute[1]} />
  const cv = path.match(/^\/cvs\/([0-9a-f-]+)$/i)
  if (cv) return <CvPage id={cv[1]} user={user} />
  return (
    <section className="surface page-surface not-found-surface">
      <h1>{t('Page not found')}</h1>
      <a className="btn btn-primary" href="/">{t('Return home')}</a>
    </section>
  )
}

function SignInPrompt() {
  return (
    <section className="surface page-surface signin-prompt-surface">
      <h1>{t('Sign in required')}</h1>
      <p>{t('Sign in to access your profile.')}</p>
      <a className="btn btn-primary" href="/login">{t('Sign in')}</a>
    </section>
  )
}

function App() {
  const [user, setUser] = useState<CurrentUser | null>(null)
  const [theme, setTheme] = useState(localStorage.getItem('talenthub_theme') || 'light')
  const [language, setLanguage] = useState(localStorage.getItem('talenthub_language') || 'en')
  const [settings, setSettings] = useState<Settings | null>(null)

  useEffect(() => {
    getCurrentUser().then(setUser).catch(() => setUser(null))
  }, [])

  useEffect(() => {
    if (!user) return
    api<Settings>('/settings').then(value => {
      setSettings(value)
      setTheme(value.theme)
      setLanguage(value.language)
    }).catch(() => undefined)
  }, [user])

  useEffect(() => {
    localStorage.setItem('talenthub_theme', theme)
    document.documentElement.setAttribute('data-bs-theme', theme)
  }, [theme])

  useEffect(() => {
    localStorage.setItem('talenthub_language', language)
    document.documentElement.lang = language
  }, [language])

  async function preference(kind: 'theme' | 'language', value: string) {
    if (kind === 'theme') {
      setTheme(value)
    } else {
      localStorage.setItem('talenthub_language', value)
      setLanguage(value)
    }
    if (!user || !settings) return
    try {
      const saved = await api<Settings>(`/settings/${kind}`, json('PUT', { value, version: settings.version }))
      setSettings(saved)
    } catch {
      setSettings(null)
    }
  }

  return (
    <div className="app">
      <Header
        user={user}
        theme={theme}
        onThemeChange={val => preference('theme', val)}
        language={language}
        onLanguageChange={val => preference('language', val)}
        onSignOut={() => signOut().catch(() => undefined).finally(() => setUser(null))}
      />
      <main className="main-content">
        <Page user={user} onAuth={setUser} />
      </main>
    </div>
  )
}

export default App
