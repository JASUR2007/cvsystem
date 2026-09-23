import { useEffect, useState } from 'react'
import AuthPage from './auth/AuthPage'
import AuthCallbackPage from './auth/AuthCallbackPage'
import { getCurrentUser, getCachedUser, signOut, type CurrentUser } from './auth/api'
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
import NotFoundPage from './pages/NotFoundPage'
import Header from './shared/Header'
import './index.css'
import './App.css'
import './media.css'
import { t } from './shared/i18n'

type Settings = { language: string; theme: string; version: number }

function Page({ path, user, onAuth }: { path: string; user: CurrentUser | null; onAuth: (user: CurrentUser) => void }) {
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
  return <NotFoundPage />
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
  const [currentPath, setCurrentPath] = useState(() => {
    const raw = window.location.pathname.replace(/\/$/, '') || '/'
    return raw === '/index.html' ? '/' : raw
  })
  const [user, setUser] = useState<CurrentUser | null>(() => getCachedUser())
  const [theme, setTheme] = useState(localStorage.getItem('talenthub_theme') || 'light')
  const [language, setLanguage] = useState(localStorage.getItem('talenthub_language') || 'en')
  const [settings, setSettings] = useState<Settings | null>(null)

  useEffect(() => {
    function handleLocation() {
      const raw = window.location.pathname.replace(/\/$/, '') || '/'
      setCurrentPath(raw === '/index.html' ? '/' : raw)
      window.scrollTo(0, 0)
    }
    window.addEventListener('popstate', handleLocation)

    function handleLinkClick(e: MouseEvent) {
      if (e.defaultPrevented || e.button !== 0 || e.metaKey || e.ctrlKey || e.shiftKey || e.altKey) return
      const anchor = (e.target as HTMLElement).closest('a')
      if (!anchor || !anchor.href) return
      if (anchor.target && anchor.target !== '_self') return
      if (anchor.hasAttribute('download')) return
      const href = anchor.getAttribute('href')
      if (!href || href.startsWith('#') || href.startsWith('mailto:') || href.startsWith('tel:')) return

      const url = new URL(anchor.href)
      if (url.origin === window.location.origin && !url.pathname.startsWith('/api')) {
        e.preventDefault()
        if (url.pathname !== window.location.pathname || url.search !== window.location.search) {
          window.history.pushState(null, '', url.pathname + url.search)
          handleLocation()
        }
      }
    }
    document.addEventListener('click', handleLinkClick)

    return () => {
      window.removeEventListener('popstate', handleLocation)
      document.removeEventListener('click', handleLinkClick)
    }
  }, [])

  useEffect(() => {
    getCurrentUser().then(setUser).catch(() => setUser(null))
    function onUserUpdate() {
      const u = getCachedUser()
      if (u) setUser(u)
    }
    window.addEventListener('talenthub_user_updated', onUserUpdate)
    return () => window.removeEventListener('talenthub_user_updated', onUserUpdate)
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
        currentPath={currentPath}
        user={user}
        theme={theme}
        onThemeChange={val => preference('theme', val)}
        language={language}
        onLanguageChange={val => preference('language', val)}
        onSignOut={() => {
          signOut().catch(() => undefined).finally(() => {
            setUser(null)
            window.location.assign('/')
          })
        }}
      />
      <main className="main-content">
        <Page path={currentPath} user={user} onAuth={setUser} />
      </main>
    </div>
  )
}

export default App
