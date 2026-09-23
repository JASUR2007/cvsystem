import { useEffect, useState, type FormEvent } from 'react'
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
import './App.css'
import './media.css'
import { t } from './shared/i18n'

type Settings = { language: string; theme: string; version: number }
const path = window.location.pathname

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
  if (path === '/admin' || path === '/admin/users') return <AdminPage />
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
  return <section className="surface page-surface"><h1>{t('Page not found')}</h1><a href="/">{t('Return home')}</a></section>
}

function SignInPrompt() {
  return <section className="surface page-surface"><h1>{t('Sign in required')}</h1><p>{t('Sign in to access your profile.')}</p><a className="btn btn-primary" href="/login">{t('Sign in')}</a></section>
}

function App() {
  const [user, setUser] = useState<CurrentUser | null>(null)
  const [theme, setTheme] = useState(localStorage.getItem('talenthub_theme') || 'light')
  const [language, setLanguage] = useState(localStorage.getItem('talenthub_language') || 'en')
  const [settings, setSettings] = useState<Settings | null>(null)
  const [search, setSearch] = useState('')
  const canManage = user?.roles.some(role => role === 'Recruiter' || role === 'Administrator') ?? false
  const isAdmin = user?.roles.includes('Administrator') ?? false

  useEffect(() => { getCurrentUser().then(setUser).catch(() => setUser(null)) }, [])
  useEffect(() => {
    if (!user) return
    api<Settings>('/settings').then(value => { setSettings(value); setTheme(value.theme); setLanguage(value.language) }).catch(() => undefined)
  }, [user])
  useEffect(() => { localStorage.setItem('talenthub_theme', theme); document.documentElement.setAttribute('data-bs-theme', theme) }, [theme])
  useEffect(() => { localStorage.setItem('talenthub_language', language); document.documentElement.lang = language }, [language])

  async function preference(kind: 'theme' | 'language', value: string) {
    if (kind === 'theme') setTheme(value)
    else { localStorage.setItem('talenthub_language', value); setLanguage(value) }
    if (!user || !settings) return
    try {
      const saved = await api<Settings>(`/settings/${kind}`, json('PUT', { value, version: settings.version }))
      setSettings(saved)
    } catch { setSettings(null) }
  }

  function submitSearch(event: FormEvent) {
    event.preventDefault()
    window.location.assign(`/search?q=${encodeURIComponent(search)}`)
  }

  return <div className="app"><header className="topbar"><a className="brand" href="/"><span className="brand-mark"><svg viewBox="0 0 24 24" aria-hidden="true"><path d="M5 4h10a4 4 0 0 1 4 4v8a4 4 0 0 1-4 4H5V4Zm3 4v8m4-8v8m4-6v4" /></svg></span>TalentHub</a><nav aria-label={t('Main navigation')}><a className={path.startsWith('/positions') ? 'active' : ''} href="/positions">{t('Positions')}</a>{canManage && <a className={path.startsWith('/attributes') ? 'active' : ''} href="/attributes">{t('Attributes')}</a>}{user && <a className={path.startsWith('/profile') ? 'active' : ''} href="/profile">{t('Profile')}</a>}{isAdmin && <a className={path.startsWith('/admin') ? 'active' : ''} href="/admin">{t('Admin')}</a>}</nav><form className="header-search" onSubmit={submitSearch}><svg viewBox="0 0 24 24" aria-hidden="true"><circle cx="11" cy="11" r="7"/><path d="m16 16 4 4"/></svg><input value={search} onChange={event => setSearch(event.target.value)} placeholder={t('Search...')} aria-label={t('Search')} /></form><div className="header-actions"><select aria-label={t('Language')} value={language} onChange={event => preference('language', event.target.value)}><option value="en">EN</option><option value="ru">RU</option><option value="uz">UZ</option></select><button className="icon-button" aria-label={t('Toggle theme')} onClick={() => preference('theme', theme === 'dark' ? 'light' : 'dark')}>{theme === 'dark' ? <svg viewBox="0 0 24 24" aria-hidden="true"><circle cx="12" cy="12" r="4"/><path d="M12 2v2m0 16v2M4.9 4.9l1.4 1.4m11.4 11.4 1.4 1.4M2 12h2m16 0h2M4.9 19.1l1.4-1.4M17.7 6.3l1.4-1.4"/></svg> : <svg viewBox="0 0 24 24" aria-hidden="true"><path d="M20.3 15.2A8.5 8.5 0 0 1 8.8 3.7 8.5 8.5 0 1 0 20.3 15.2Z"/></svg>}</button>{user ? <><span className="user-name">{user.firstName}</span><button className="btn btn-sm btn-outline-primary" onClick={() => signOut().catch(() => undefined).finally(() => setUser(null))}>{t('Sign out')}</button></> : <a className="btn btn-sm btn-primary" href="/login">{t('Sign in')}</a>}</div></header><main className="main-content"><Page user={user} onAuth={setUser} /></main></div>
}

export default App
